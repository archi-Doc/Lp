// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Netsphere.Core;
using Netsphere.Packet;

namespace Netsphere.Relay;

/// <summary>
/// Manages relays and conducts the actual relay processing on the server side.
/// </summary>
public partial class RelayAgent
{
    private const int EndPointCacheSize = 100;

    [ValueLinkObject]
    private partial class NetAddressToEndPointItem
    {
        [Link(Primary = true, Type = ChainType.QueueList, Name = "Queue")]
        public NetAddressToEndPointItem(NetAddress netAddress, bool known)
        {
            this.NetAddress = netAddress;
            netAddress.CreateIPEndPoint(out var endPoint);
            this.EndPoint = endPoint;
            this.Known = known;
        }

        [Link(Type = ChainType.Unordered, AddValue = false)]
        public NetAddress NetAddress { get; }

        public IPEndPoint EndPoint { get; }

        public bool Known { get; }
    }

    internal RelayAgent(IRelayControl relayControl, NetTerminal netTerminal)
    {
        this.relayControl = relayControl;
        this.netTerminal = netTerminal;
    }

    #region FieldAndProperty

    private readonly IRelayControl relayControl;
    private readonly NetTerminal netTerminal;

    private readonly RelayExchange.GoshujinClass items = new();
    private readonly Aes aes = Aes.Create();

    private readonly NetAddressToEndPointItem.GoshujinClass endPointCache = new();
    private readonly ConcurrentQueue<NetSender.Item> sendItems = new();

    #endregion

    public RelayResult Add(ServerConnection serverConnection, out ushort relayId)
    {
        relayId = 0;
        ushort outerRelayId = 0;
        lock (this.items.SyncObject)
        {
            if (this.items.Count > this.relayControl.MaxParallelRelays)
            {
                return RelayResult.ParallelRelayLimit;
            }

            while (true)
            {
                relayId = (ushort)RandomVault.Pseudo.NextUInt32();
                if (!this.items.RelayIdChain.ContainsKey(relayId))
                {
                    break;
                }
            }

            while (true)
            {
                outerRelayId = (ushort)RandomVault.Pseudo.NextUInt32();
                if (!this.items.RelayIdChain.ContainsKey(outerRelayId))
                {
                    break;
                }
            }

            this.items.Add(new(relayId, outerRelayId, serverConnection));
        }

        return RelayResult.Success;
    }

    public bool ProcessRelay(NetEndpoint endpoint, ushort destinationRelayId, ByteArrayPool.MemoryOwner source, out ByteArrayPool.MemoryOwner decrypted)
    {// This is all the code that performs the actual relay processing.
        var span = source.Span.Slice(RelayHeader.RelayIdLength);
        if (source.Owner is null)
        {// Invalid data
            goto Exit;
        }

        RelayExchange? exchange;
        lock (this.items.SyncObject)
        {
            exchange = this.items.RelayIdChain.FindFirst(destinationRelayId);
            if (exchange is null || !exchange.DecrementAndCheck())
            {// No relay exchange
                goto Exit;
            }
        }

        if (exchange.RelayId == destinationRelayId)
        {// InnerRelayId
            if (exchange.Endpoint.EndPointEquals(endpoint))
            {// Inner -> Outer: Decrypt
                if ((span.Length & 15) != 0)
                {// Invalid data
                    goto Exit;
                }

                this.aes.Key = exchange.Key;
                if (!this.aes.TryDecryptCbc(span, exchange.Iv, span, out var written, PaddingMode.None) ||
                    written < RelayHeader.Length)
                {
                    goto Exit;
                }

                var relayHeader = MemoryMarshal.Read<RelayHeader>(span);
                if (relayHeader.Zero == 0)
                { // Decrypted. Process the packet on this node.
                    span = span.Slice(RelayHeader.Length - RelayHeader.RelayIdLength);
                    decrypted = source.Owner.ToMemoryOwner(RelayHeader.Length, RelayHeader.RelayIdLength + written - RelayHeader.Length - relayHeader.PaddingLength);
                    if (relayHeader.NetAddress == NetAddress.Relay)
                    {// Initiator -> This node
                        MemoryMarshal.Write(span, endpoint.RelayId); // SourceRelayId
                        span = span.Slice(sizeof(ushort));
                        MemoryMarshal.Write(span, (ushort)0); // DestinationRelayId
                        return true;
                    }
                    else
                    {// Initiator -> Other (known)
                        if (exchange.OuterEndpoint.IsValid)
                        {// Inner relay
                            goto Exit;
                        }

                        MemoryMarshal.Write(span, exchange.OuterRelayId); // SourceRelayId
                        span = span.Slice(sizeof(ushort));
                        MemoryMarshal.Write(span, relayHeader.NetAddress.RelayId); // DestinationRelayId

                        var ep2 = this.GetEndPoint_NotThreadSafe(relayHeader.NetAddress, true);
                        decrypted.IncrementAndShare();
                        this.sendItems.Enqueue(new(ep2.EndPoint, decrypted));
                    }
                }
                else
                {// Not decrypted. Relay the packet to the next node.
                    if (exchange.OuterEndpoint.IsValid)
                    {// -> Outer relay
                        MemoryMarshal.Write(source.Span, exchange.OuterRelayId);
                        MemoryMarshal.Write(source.Span.Slice(sizeof(ushort)), exchange.OuterEndpoint.RelayId);
                        source.IncrementAndShare();
                        this.sendItems.Enqueue(new(exchange.OuterEndpoint.EndPoint, source));
                    }
                    else
                    {// No outer relay. Discard
                    }
                }
            }
            else
            {// Invalid: Discard
            }
        }
        else
        {// OuterRelayId
            if (exchange.OuterEndpoint.IsValid)
            {// Inner relay
                if (exchange.OuterEndpoint.EndPointEquals(endpoint))
                {// Outer relay -> Inner: Encrypt
                }
                else
                {// Other (known or unknown)
                    goto Exit;
                }
            }
            else
            {// Outermost relay
                // Other (known, unknown)
                //var ep2 = this.GetEndPoint_NotThreadSafe(endpoint, false);
            }

            this.aes.Key = exchange.Key;
            var sourceRelayId = MemoryMarshal.Read<ushort>(source.Span);
            if (sourceRelayId == 0)
            {// RelayId(Source/Destination), RelayHeader, Content, Padding
                var sourceSpan = source.Owner.ByteArray.AsSpan(RelayHeader.RelayIdLength);
                span.CopyTo(sourceSpan.Slice(RelayHeader.Length));

                var contentLength = span.Length;
                var multiple = contentLength & ~15;
                var paddingLength = contentLength == multiple ? 0 : (multiple + 16 - contentLength);

                // RelayHeader
                var relayHeader = new RelayHeader(RandomVault.Crypto.NextUInt32(), (byte)paddingLength, new(endpoint));
                MemoryMarshal.Write(sourceSpan, relayHeader);
                sourceSpan = sourceSpan.Slice(RelayHeader.Length);

                sourceSpan = sourceSpan.Slice(contentLength);
                sourceSpan.Slice(0, paddingLength).Fill(0x07);

                source = new(source.Owner.ByteArray, 0, RelayHeader.RelayIdLength + RelayHeader.Length + contentLength + paddingLength);
                span = source.Span.Slice(RelayHeader.RelayIdLength);
            }

            // Encrypt
            if ((span.Length & 15) != 0)
            {// Invalid data
                goto Exit;
            }

            this.aes.Key = exchange.Key;
            if (!this.aes.TryEncryptCbc(span, exchange.Iv, span, out _, PaddingMode.None))
            {
                goto Exit;
            }

            MemoryMarshal.Write(source.Span, exchange.RelayId); // SourceRelayId
            MemoryMarshal.Write(source.Span.Slice(sizeof(ushort)), exchange.Endpoint.RelayId); // DestinationRelayId
            source.IncrementAndShare();
            this.sendItems.Enqueue(new(exchange.Endpoint.EndPoint, source));
        }

Exit:
        decrypted = default;
        return false;
    }

    internal void ProcessSend(NetSender netSender)
    {
        while (netSender.CanSend &&
            this.sendItems.TryDequeue(out var item))
        {
            netSender.Send_NotThreadSafe(item.EndPoint, item.MemoryOwner);
        }
    }

    internal (IPEndPoint EndPoint, bool Known) GetEndPoint_NotThreadSafe(NetAddress netAddress, bool known)
    {
        if (!this.endPointCache.NetAddressChain.TryGetValue(netAddress, out var item))
        {
            item = new(netAddress, known);
            this.endPointCache.Add(item);
            if (this.endPointCache.Count > EndPointCacheSize)
            {
                this.endPointCache.QueueChain.Peek().Goshujin = default;
            }
        }

        return (item.EndPoint, item.Known);
    }

    internal PingRelayResponse? ProcessPingRelay(ushort destinationRelayId)
    {
        if (this.items.Count == 0)
        {
            return null;
        }

        RelayExchange? exchange;
        lock (this.items.SyncObject)
        {
            exchange = this.items.RelayIdChain.FindFirst(destinationRelayId);
            if (exchange is null)
            {
                return null;
            }
        }

        var packet = new PingRelayResponse(exchange.RelayPoint, exchange.OuterEndpoint.EndPoint);
        return packet;
    }

    internal SetRelayResponse? ProcessSetRelay(ushort destinationRelayId, SetRelayPacket p)
    {
        if (this.items.Count == 0)
        {
            return null;
        }

        RelayExchange? exchange;
        lock (this.items.SyncObject)
        {
            exchange = this.items.RelayIdChain.FindFirst(destinationRelayId);
            if (exchange is null)
            {
                return null;
            }

            exchange.OuterEndpoint = p.OuterEndPoint;
        }

        var packet = new SetRelayResponse();
        packet.Result = RelayResult.Success;
        return packet;
    }

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Aes RentAesInternal()
    {
        Aes aes;
        if (this.aes0 is not null)
        {
            aes = this.aes0;
            this.aes0 = this.aes1;
            this.aes1 = default;
            return aes;
        }
        else
        {
            aes = Aes.Create();
            aes.KeySize = 256;
            return aes;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnAesInternal(Aes aes)
    {
        if (this.aes0 is null)
        {
            this.aes0 = aes;
            return;
        }
        else if (this.aes1 is null)
        {
            this.aes1 = aes;
            return;
        }
        else
        {
            aes.Dispose();
        }
    }*/
}
