﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    [ValueLinkObject]
    private partial class Item
    {
        public enum Type
        {
            Inner,
            Outer,
        }

        public Item(ushort relayId, ServerConnection serverConnection)
        {
            this.RelayId = relayId;
            this.Endpoint = serverConnection.DestinationEndpoint;

            this.Key = new byte[Connection.EmbryoKeyLength];
            serverConnection.UnsafeCopyKey(this.Key);
            this.Iv = new byte[Connection.EmbryoIvLength];
            serverConnection.UnsafeCopyIv(this.Iv);
        }

        // public Type RelayType { get; private set; }

        [Link(Primary = true, Type = ChainType.Unordered, AddValue = false)]
        public ushort RelayId { get; private set; }

        [Link(UnsafeTargetChain = "RelayIdChain", AutoLink = false, AddValue = false)]
        public ushort OuterRelayId { get; private set; }

        // [Link(Type = ChainType.Unordered, AddValue = false)]
        public NetEndpoint Endpoint { get; private set; }

        public NetEndpoint OuterEndpoint { get; private set; }

        public long RelayPoint { get; private set; } = 1_000_000; // tempcode

        internal byte[] Key { get; private set; }

        internal byte[] Iv { get; private set; }

        public bool DecrementAndCheck()
        {
            if (this.RelayPoint-- <= 0)
            {
                this.Clean();
                this.Goshujin = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Clean()
        {
        }
    }

    internal RelayAgent(IRelayControl relayControl, NetTerminal netTerminal)
    {
        this.relayControl = relayControl;
        this.netTerminal = netTerminal;
    }

    #region FieldAndProperty

    private readonly IRelayControl relayControl;
    private readonly NetTerminal netTerminal;

    private readonly object syncObject = new();
    private readonly Item.GoshujinClass items = new();
    private Aes? aes0;
    private Aes? aes1;

    #endregion

    public RelayResult Add(ushort relayId, ServerConnection serverConnection)
    {
        lock (this.syncObject)
        {
            if (this.items.Count > this.relayControl.MaxParallelRelays)
            {
                return RelayResult.ParallelRelayLimit;
            }

            if (this.items.RelayIdChain.ContainsKey(relayId))
            {
                return RelayResult.DuplicateRelayId;
            }

            this.items.Add(new(relayId, serverConnection));
        }

        return RelayResult.Success;
    }

    public bool ProcessReceive(NetEndpoint endpoint, ByteArrayPool.MemoryOwner source, out ByteArrayPool.MemoryOwner decrypted)
    {
        var span = source.Span.Slice(sizeof(ushort));
        if ((span.Length & 15) != 0 ||
            source.Owner is null)
        {
            goto Exit;
        }

        Item? item;
        Aes? aes;
        lock (this.syncObject)
        {
            item = this.items.RelayIdChain.FindFirst(endpoint.RelayId);
            if (item is null || !item.DecrementAndCheck())
            {
                goto Exit;
            }

            aes = this.RentAesInternal();
        }

        try
        {
            if (endpoint.RelayId == item.RelayId)
            {// Inner -> Outer
                if (item.Endpoint.EndPointEquals(endpoint))
                {// Inner -> Outer: Decrypt
                    aes.Key = item.Key;
                    if (!aes.TryDecryptCbc(span, item.Iv, span, out var written, PaddingMode.None) ||
                        written < RelayHeader.Length)
                    {
                        goto Exit;
                    }

                    var relayHeader = MemoryMarshal.Read<RelayHeader>(span);
                    if (relayHeader.Zero == 0)
                    { // Decrypted
                        span = span.Slice(RelayHeader.Length - sizeof(ushort));
                        MemoryMarshal.Write(span, relayHeader.NetAddress.RelayId);
                        decrypted = source.Owner.ToMemoryOwner(RelayHeader.Length, sizeof(ushort) + written - relayHeader.PaddingLength);
                        if (relayHeader.NetAddress == NetAddress.Relay)
                        {// Initiator -> This node
                            return true;
                        }
                        else
                        {// Initiator -> Other
                            if (this.netTerminal.TryCreateEndpoint(relayHeader.NetAddress, out var ep2))
                            {
                                this.netTerminal.NetSender.Send_NotThreadSafe(ep2.EndPoint, decrypted);
                                this.netTerminal.Flag = true;
                            }
                        }
                    }
                    else
                    {// Not decrypted
                        if (item.OuterRelayIdLink.IsLinked)
                        {// -> Outer relay
                            MemoryMarshal.Write(source.Span, item.OuterRelayId);
                            this.netTerminal.NetSender.Send_NotThreadSafe(item.OuterEndpoint.EndPoint, source);
                        }
                        else
                        {// Discard
                        }
                    }
                }
                else
                {// Invalid: Discard
                }
            }
            else
            {// Outer -> Inner
                if (item.Endpoint.EndPointEquals(endpoint))
                {// Outer relay -> Inner: Encrypt
                    aes.Key = item.Key;
                }
                else
                {// Other
                 // Known
                 // Unknown
                }
            }
        }
        finally
        {
            lock (this.syncObject)
            {
                this.ReturnAesInternal(aes);
            }
        }

Exit:
        decrypted = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    }
}
