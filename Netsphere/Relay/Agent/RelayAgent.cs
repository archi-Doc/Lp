// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Netsphere.Core;

namespace Netsphere.Relay;

/// <summary>
/// Manages relays and conducts the actual relay processing on the server side.
/// </summary>
public partial class RelayAgent
{
    private const long CleanIntervalMics = 10_000_000; // 10 seconds
    private const long UnrestrictedRetensionMics = 60_000_000; // 1 minute
    private const int EndPointCacheSize = 100;

    internal enum EndPointOperation
    {
        None,
        Update,
        SetUnrestricted,
        SetRestricted,
    }

    [ValueLinkObject]
    private partial class EndPointItem
    {
        [Link(Primary = true, Name = "LinkedList", Type = ChainType.LinkedList)]
        public EndPointItem(NetAddress netAddress)
        {
            this.NetAddress = netAddress;
            netAddress.CreateIPEndPoint(out var endPoint);
            this.EndPoint = endPoint;
        }

        [Link(Type = ChainType.Unordered, AddValue = false)]
        public NetAddress NetAddress { get; }

        public IPEndPoint EndPoint { get; }

        public long UnrestrictedMics { get; internal set; }

        public bool IsUnrestricted
        {
            get
            {
                if (this.UnrestrictedMics == 0)
                {
                    return false;
                }
                else if (Mics.FastSystem - this.UnrestrictedMics > UnrestrictedRetensionMics)
                {
                    this.UnrestrictedMics = 0;
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }

    internal RelayAgent(IRelayControl relayControl, NetTerminal netTerminal)
    {
        this.relayControl = relayControl;
        this.netTerminal = netTerminal;
    }

    #region FieldAndProperty

    public int NumberOfExchanges
        => this.items.Count;

    private readonly IRelayControl relayControl;
    private readonly NetTerminal netTerminal;

    private readonly RelayExchange.GoshujinClass items = new();
    private readonly Aes aes = Aes.Create();

    private readonly EndPointItem.GoshujinClass endPointCache = new();
    private readonly ConcurrentQueue<NetSender.Item> sendItems = new();

    private long lastCleanMics;
    private long lastRestrictedMics;

    #endregion

    public RelayResult Add(ServerConnection serverConnection, out ushort relayId)
    {
        relayId = 0;
        ushort outerRelayId = 0;
        lock (this.items.SyncObject)
        {
            if (this.NumberOfExchanges >= this.relayControl.MaxParallelRelays)
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

            this.items.Add(new(this.relayControl, relayId, outerRelayId, serverConnection));
        }

        return RelayResult.Success;
    }

    public long AddRelayPoint(ushort relayId, long relayPoint)
    {
        lock (this.items.SyncObject)
        {
            var exchange = this.items.RelayIdChain.FindFirst(relayId);
            if (exchange is null)
            {
                return 0;
            }

            var prev = exchange.RelayPoint;
            exchange.RelayPoint += relayPoint;
            if (exchange.RelayPoint > this.relayControl.DefaultMaxRelayPoint)
            {
                exchange.RelayPoint = this.relayControl.DefaultMaxRelayPoint;
            }

            return exchange.RelayPoint - prev;
        }
    }

    public void Clean()
    {
        if (Mics.FastSystem - this.lastCleanMics < CleanIntervalMics)
        {
            return;
        }

        this.lastCleanMics = Mics.FastSystem;

        lock (this.items.SyncObject)
        {
            Queue<RelayExchange>? toDelete = default;
            foreach (var x in this.items)
            {
                if (this.lastCleanMics - x.LastAccessMics > x.RelayRetensionMics)
                {
                    toDelete ??= new();
                    toDelete.Enqueue(x);
                }
            }

            if (toDelete is not null)
            {
                while (toDelete.TryDequeue(out var x))
                {
                    this.items.Remove(x);
                }
            }
        }
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
                    {// Initiator -> Other (unrestricted)
                        if (exchange.OuterEndpoint.IsValid)
                        {// Inner relay
                            goto Exit;
                        }

                        MemoryMarshal.Write(span, exchange.OuterRelayId); // SourceRelayId
                        span = span.Slice(sizeof(ushort));
                        MemoryMarshal.Write(span, relayHeader.NetAddress.RelayId); // DestinationRelayId

                        var operation = EndPointOperation.Update;
                        var packetType = MemoryMarshal.Read<Netsphere.Packet.PacketType>(span.Slice(6));
                        if (packetType == Packet.PacketType.Connect)
                        {// Connect
                            operation = EndPointOperation.SetUnrestricted;
                        }
                        else
                        {// Other
                            operation = EndPointOperation.Update;
                        }

                        // Close -> EndPointOperation.SetRestricted ?

                        var ep2 = this.GetEndPoint_NotThreadSafe(relayHeader.NetAddress, operation);
                        decrypted.IncrementAndShare();
                        this.sendItems.Enqueue(new(ep2.EndPoint, decrypted));
                        // Console.WriteLine($"Outermost[{decrypted.Memory.Length}] {endpoint} to {relayHeader.NetAddress}");
                    }
                }
                else
                {// Not decrypted. Relay the packet to the next node.
                    if (exchange.OuterEndpoint.EndPoint is { } ep)
                    {// -> Outer relay
                        MemoryMarshal.Write(source.Span, exchange.OuterRelayId);
                        MemoryMarshal.Write(source.Span.Slice(sizeof(ushort)), exchange.OuterEndpoint.RelayId);
                        source.IncrementAndShare();
                        this.sendItems.Enqueue(new(ep, source));
                        // Console.WriteLine($"Inner->Outer[{source.Memory.Length}] {endpoint} to {exchange.OuterEndpoint}");
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
                {// Other (unrestricted or restricted)
                    goto Exit;
                }
            }
            else
            {// Outermost relay
                // Other (unrestricted or restricted)
                var ep2 = this.GetEndPoint_NotThreadSafe(new(endpoint), EndPointOperation.None);
                if (!ep2.Unrestricted)
                {// Restricted
                    if (exchange.RestrictedIntervalMics == 0 ||
                        Mics.FastSystem - this.lastRestrictedMics < exchange.RestrictedIntervalMics)
                    {// Discard
                        goto Exit;
                    }

                    this.lastRestrictedMics = Mics.FastSystem;
                }
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

                source = source.Owner.ToMemoryOwner(0, RelayHeader.RelayIdLength + RelayHeader.Length + contentLength + paddingLength);
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

            if (exchange.Endpoint.EndPoint is { } ep)
            {
                MemoryMarshal.Write(source.Span, exchange.RelayId); // SourceRelayId
                MemoryMarshal.Write(source.Span.Slice(sizeof(ushort)), exchange.Endpoint.RelayId); // DestinationRelayId
                source.IncrementAndShare();
                this.sendItems.Enqueue(new(ep, source));
                // Console.WriteLine($"Outer->Inner[{source.Memory.Length}] {endpoint} to {exchange.Endpoint}");
            }
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

    internal (IPEndPoint EndPoint, bool Unrestricted) GetEndPoint_NotThreadSafe(NetAddress netAddress, EndPointOperation operation)
    {
        if (!this.endPointCache.NetAddressChain.TryGetValue(netAddress, out var item))
        {
            item = new(netAddress);
            this.endPointCache.Add(item);
            if (this.endPointCache.Count > EndPointCacheSize &&
                this.endPointCache.LinkedListChain.First is { } i)
            {
                i.Goshujin = default;
            }
        }
        else
        {
            this.endPointCache.LinkedListChain.AddLast(item);
        }

        var unrestricted = false;
        if (operation == EndPointOperation.SetUnrestricted)
        {// Unrestricted
            unrestricted = true;
            item.UnrestrictedMics = Mics.FastSystem;
        }
        else if (operation == EndPointOperation.SetRestricted)
        {// Restricted
            item.UnrestrictedMics = 0;
        }
        else
        {// None, Update
            if (Mics.FastSystem - item.UnrestrictedMics > UnrestrictedRetensionMics)
            {// Restricted
                item.UnrestrictedMics = 0;
            }
            else
            {// Unrestricted
                unrestricted = true;
                if (operation == EndPointOperation.Update)
                {
                    item.UnrestrictedMics = Mics.FastSystem;
                }
            }
        }

        return (item.EndPoint, unrestricted);
    }

    internal PingRelayResponse? ProcessPingRelay(ushort destinationRelayId)
    {
        if (this.NumberOfExchanges == 0)
        {
            return null;
        }

        RelayExchange? exchange;
        PingRelayResponse? packet;
        lock (this.items.SyncObject)
        {
            exchange = this.items.RelayIdChain.FindFirst(destinationRelayId);
            if (exchange is null)
            {
                return null;
            }

            packet = new PingRelayResponse(exchange);
        }

        return packet;
    }

    internal SetRelayResponse? ProcessSetRelay(ushort destinationRelayId, SetRelayPacket p)
    {
        if (this.NumberOfExchanges == 0)
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
