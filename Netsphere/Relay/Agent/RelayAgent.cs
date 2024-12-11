﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
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
        this.logger = this.netTerminal.UnitLogger.GetLogger<RelayAgent>();
    }

    #region FieldAndProperty

    public int NumberOfExchanges
        => this.items.Count;

    private readonly ILogger logger;
    private readonly IRelayControl relayControl;
    private readonly NetTerminal netTerminal;

    private readonly RelayExchange.GoshujinClass items = new();

    private readonly EndPointItem.GoshujinClass endPointCache = new();
    private readonly ConcurrentQueue<NetSender.Item> sendItems = new();

    private long lastCleanMics;
    private long lastRestrictedMics;

    #endregion

    public override string ToString()
    {
        var sb = new StringBuilder();
        using (this.items.LockObject.EnterScope())
        {
            foreach (var x in this.items)
            {
                sb.AppendLine($"[{x.RelayId}]{x.Endpoint} - [{x.OuterRelayId}]{x.OuterEndpoint}");
            }
        }

        return sb.ToString();
    }

    public RelayResult Add(ServerConnection serverConnection, CreateRelayBlock block, out RelayId relayId, out RelayId outerRelayId)
    {
        relayId = 0;
        outerRelayId = 0;
        using (this.items.LockObject.EnterScope())
        {
            if (this.NumberOfExchanges >= this.relayControl.MaxParallelRelays)
            {
                return RelayResult.ParallelRelayLimit;
            }

            while (true)
            {
                relayId = (RelayId)RandomVault.Default.NextUInt32();
                if (!this.items.RelayIdChain.ContainsKey(relayId))
                {
                    break;
                }
            }

            while (true)
            {
                outerRelayId = (RelayId)RandomVault.Default.NextUInt32();
                if (!this.items.RelayIdChain.ContainsKey(outerRelayId))
                {
                    break;
                }
            }

            this.items.Add(new(this.relayControl, relayId, outerRelayId, serverConnection, block));
        }

        return RelayResult.Success;
    }

    public long AddRelayPoint(RelayId relayId, long relayPoint)
    {
        using (this.items.LockObject.EnterScope())
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

        using (this.items.LockObject.EnterScope())
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

    public bool ProcessRelay(NetEndpoint endpoint, RelayId destinationRelayId, BytePool.RentMemory source, out BytePool.RentMemory decrypted)
    {// This is all the code that performs the actual relay processing.
        var span = source.Span.Slice(RelayHeader.RelayIdLength);
        if (source.RentArray is null)
        {// Invalid data
            goto Exit;
        }

        RelayExchange? exchange;
        using (this.items.LockObject.EnterScope())
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
                if (span.Length < (RelayHeader.Length - RelayHeader.RelayIdLength))
                {// Invalid data
                    goto Exit;
                }

                var salt4 = MemoryMarshal.Read<uint>(span);
                var headerSpan = span;
                span = span.Slice(sizeof(uint));

                Span<byte> nonce32 = stackalloc byte[32];
                RelayHelper.CreateNonce(salt4, exchange.EmbryoSalt, exchange.EmbryoSecret, nonce32);
                if (!Aegis256.TryDecrypt(span, span, nonce32, exchange.EmbryoKey, default, 0))
                {
                    if (NetConstants.LogRelay)
                    {
                        this.logger.TryGet(LogLevel.Information)?.Log($"Inner({endpoint}) : decryption failue");
                    }

                    goto Exit;
                }

                var relayHeader = MemoryMarshal.Read<RelayHeader>(headerSpan);
                if (relayHeader.Zero == 0)
                { // Decrypted. Process the packet on this node.
                    span = span.Slice(RelayHeader.Length - RelayHeader.PlainLength - RelayHeader.RelayIdLength);
                    decrypted = source.Slice(RelayHeader.Length);
                    // decrypted = source.RentArray.AsMemory(RelayHeader.Length, span.Length);
                    if (relayHeader.NetAddress == NetAddress.Relay)
                    {// Initiator -> This node
                        MemoryMarshal.Write(span, endpoint.RelayId); // SourceRelayId
                        span = span.Slice(sizeof(RelayId));
                        MemoryMarshal.Write(span, destinationRelayId); // DestinationRelayId

                        if (NetConstants.LogRelay)
                        {
                            this.logger.TryGet(LogLevel.Information)?.Log($"Inner({endpoint}) -> this({destinationRelayId})");
                        }

                        return true;
                    }
                    else
                    {// Initiator -> Other (unrestricted)
                        if (exchange.OuterEndpoint.IsValid)
                        {// Inner relay
                            if (NetConstants.LogRelay)
                            {
                                this.logger.TryGet(LogLevel.Information)?.Log($"Inner({endpoint}) -> this({destinationRelayId}): OuterEndpoint is already set");
                            }

                            goto Exit;
                        }

                        MemoryMarshal.Write(span, exchange.OuterRelayId); // SourceRelayId
                        span = span.Slice(sizeof(RelayId));
                        MemoryMarshal.Write(span, relayHeader.NetAddress.RelayId); // DestinationRelayId

                        var operation = EndPointOperation.Update;
                        var packetType = MemoryMarshal.Read<Netsphere.Packet.PacketType>(span.Slice(sizeof(RelayId) + sizeof(uint)));
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

                        if (NetConstants.LogRelay)
                        {
                            this.logger.TryGet(LogLevel.Information)?.Log($"Inner({endpoint}) -> this({destinationRelayId}) -> {relayHeader.NetAddress}: {decrypted.Memory.Length} bytes");
                        }
                    }
                }
                else
                {// Not decrypted. Relay the packet to the next node.
                    if (exchange.OuterEndpoint.EndPoint is { } ep)
                    {// -> Outer relay
                        MemoryMarshal.Write(source.Span, exchange.OuterRelayId);
                        MemoryMarshal.Write(source.Span.Slice(sizeof(RelayId)), exchange.OuterEndpoint.RelayId);
                        source.IncrementAndShare();
                        this.sendItems.Enqueue(new(ep, source));

                        if (NetConstants.LogRelay)
                        {
                            this.logger.TryGet(LogLevel.Information)?.Log($"Inner({endpoint}) -> this({destinationRelayId}) -> Outer({exchange.OuterEndpoint}): {source.Memory.Length} bytes");
                        }
                    }
                    else
                    {// No outer relay. Discard
                        if (NetConstants.LogRelay)
                        {
                            this.logger.TryGet(LogLevel.Information)?.Log($"Inner({endpoint}) -> relay: No outer relay");
                        }
                    }
                }
            }
            else
            {// Invalid: Discard
                if (NetConstants.LogRelay)
                {
                    this.logger.TryGet(LogLevel.Information)?.Log($"Inner({endpoint}) : invalid");
                }
            }
        }
        else
        {// OuterRelayId
            if (exchange.OuterEndpoint.IsValid)
            {// Not outermost relay
                if (exchange.OuterEndpoint.EndPointEquals(endpoint))
                {// Outer relay -> Inner: Encrypt
                }
                else
                {// Other (unrestricted or restricted)
                    if (NetConstants.LogRelay)
                    {// Packets from endpoints other than the outer relay are not accepted.
                        this.logger.TryGet(LogLevel.Information)?.Log($"Outer({endpoint}) : not accepted");
                    }

                    goto Exit;
                }
            }
            else
            {// Outermost relay
                // Other (unrestricted or restricted)
                var ep2 = this.GetEndPoint_NotThreadSafe(new(endpoint), EndPointOperation.None);
                if (!ep2.Unrestricted)
                {// Restricted
                    if (!exchange.AllowUnknownNode ||
                        exchange.RestrictedIntervalMics == 0 ||
                        Mics.FastSystem - this.lastRestrictedMics < exchange.RestrictedIntervalMics)
                    {// Discard
                        if (NetConstants.LogRelay)
                        {// Packets from endpoints other than the outer relay are not accepted.
                            this.logger.TryGet(LogLevel.Information)?.Log($"Outermost({endpoint}) : discard");
                        }

                        goto Exit;
                    }

                    this.lastRestrictedMics = Mics.FastSystem;
                }
            }

            var sourceRelayId = MemoryMarshal.Read<RelayId>(source.Span);
            if (sourceRelayId == 0)
            {// RelayId(Source/Destination), RelayHeader, Content
                var sourceSpan = source.RentArray.Array.AsSpan(RelayHeader.RelayIdLength);
                span.CopyTo(sourceSpan.Slice(RelayHeader.Length));

                var contentLength = span.Length;

                // RelayHeader
                var relayHeader = new RelayHeader(RandomVault.Default.NextUInt32(), new(endpoint));
                MemoryMarshal.Write(sourceSpan, relayHeader);
                sourceSpan = sourceSpan.Slice(RelayHeader.Length);

                sourceSpan = sourceSpan.Slice(contentLength);

                source = source.RentArray.AsMemory(0, RelayHeader.RelayIdLength + RelayHeader.Length + contentLength);
                span = source.Span.Slice(RelayHeader.RelayIdLength);
            }

            // Encrypt
            var salt4 = MemoryMarshal.Read<uint>(span);
            span = span.Slice(sizeof(uint));
            Span<byte> nonce32 = stackalloc byte[32];
            RelayHelper.CreateNonce(salt4, exchange.EmbryoSalt, exchange.EmbryoSecret, nonce32);
            Aegis256.Encrypt(span, span, nonce32, exchange.EmbryoKey, default, 0);

            if (exchange.Endpoint.EndPoint is { } ep)
            {
                MemoryMarshal.Write(source.Span, exchange.RelayId); // SourceRelayId
                MemoryMarshal.Write(source.Span.Slice(sizeof(RelayId)), exchange.Endpoint.RelayId); // DestinationRelayId
                source.IncrementAndShare();
                this.sendItems.Enqueue(new(ep, source));

                if (NetConstants.LogRelay)
                {// Packets from endpoints other than the outer relay are not accepted.
                    this.logger.TryGet(LogLevel.Information)?.Log($"Outer({endpoint}) -> this({destinationRelayId}) -> {exchange.Endpoint} : {source.Memory.Length} bytes");
                }
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

    internal PingRelayResponse? ProcessPingRelay(RelayId destinationRelayId)
    {
        if (this.NumberOfExchanges == 0)
        {
            return null;
        }

        RelayExchange? exchange;
        PingRelayResponse? packet;
        using (this.items.LockObject.EnterScope())
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

    internal RelayOperatioResponse? ProcessRelayOperation(RelayId destinationRelayId, RelayOperatioPacket p)
    {
        if (this.NumberOfExchanges == 0)
        {
            return null;
        }

        RelayExchange? exchange;
        RelayOperatioResponse? response = default;
        using (this.items.LockObject.EnterScope())
        {
            exchange = this.items.RelayIdChain.FindFirst(destinationRelayId);
            if (exchange is null)
            {
                return null;
            }

            if (p.RelayOperation == RelayOperatioPacket.Operation.SetOuterEndPoint)
            {
                exchange.OuterEndpoint = p.OuterEndPoint;
                response = new(RelayResult.Success);
            }
            else if (p.RelayOperation == RelayOperatioPacket.Operation.Close)
            {
                exchange.Goshujin = null;
                exchange.Clean();
            }
        }

        return response;
    }
}
