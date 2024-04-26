// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Netsphere.Relay;

/// <summary>
/// Manages relays and conducts the actual relay processing on the server side.
/// </summary>
public partial class RelayAgent
{
    internal RelayAgent(IRelayControl relayControl, NetTerminal netTerminal)
    {
        this.relayControl = relayControl;
        this.netTerminal = netTerminal;
    }

    #region FieldAndProperty

    private readonly IRelayControl relayControl;
    private readonly NetTerminal netTerminal;

    private readonly object syncObject = new();
    private readonly RelayExchange.GoshujinClass items = new();
    private Aes? aes0;
    private Aes? aes1;

    #endregion

    public RelayResult Add(ServerConnection serverConnection, out ushort relayId)
    {
        relayId = 0;
        ushort outerRelayId = 0;
        lock (this.syncObject)
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
    {
        var span = source.Span.Slice(RelayHeader.RelayIdLength);
        if (source.Owner is null)
        {// Invalid data
            goto Exit;
        }

        RelayExchange? exchange;
        Aes? aes;
        lock (this.syncObject)
        {
            exchange = this.items.RelayIdChain.FindFirst(destinationRelayId);
            if (exchange is null || !exchange.DecrementAndCheck())
            {// No relay exchange
                goto Exit;
            }

            aes = this.RentAesInternal();
        }

        try
        {
            if (exchange.RelayId == destinationRelayId)
            {// InnerRelayId
                if (exchange.Endpoint.EndPointEquals(endpoint))
                {// Inner -> Outer: Decrypt
                    if ((span.Length & 15) != 0)
                    {// Invalid data
                        goto Exit;
                    }

                    aes.Key = exchange.Key;
                    if (!aes.TryDecryptCbc(span, exchange.Iv, span, out var written, PaddingMode.None) ||
                        written < RelayHeader.Length)
                    {
                        goto Exit;
                    }

                    var relayHeader = MemoryMarshal.Read<RelayHeader>(span);
                    if (relayHeader.Zero == 0)
                    { // Decrypted
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
                            MemoryMarshal.Write(span, exchange.OuterRelayId); // SourceRelayId
                            span = span.Slice(sizeof(ushort));
                            MemoryMarshal.Write(span, relayHeader.NetAddress.RelayId); // DestinationRelayId

                            if (this.netTerminal.TryCreateEndpoint(relayHeader.NetAddress, out var ep2))
                            {
                                decrypted.IncrementAndShare();
                                this.netTerminal.NetSender.Send_NotThreadSafe(ep2.EndPoint, decrypted);//
                            }
                        }
                    }
                    else
                    {// Not decrypted
                        if (exchange.OuterEndpoint.IsValid)
                        {// -> Outer relay
                            MemoryMarshal.Write(source.Span, exchange.OuterRelayId);
                            source.IncrementAndShare();
                            this.netTerminal.NetSender.Send_NotThreadSafe(exchange.OuterEndpoint.EndPoint, source);
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
            {// OuterRelayId
                if (exchange.OuterEndpoint.IsValid &&
                    exchange.OuterEndpoint.EndPointEquals(endpoint))
                {// Outer relay -> Inner: Encrypt
                    aes.Key = exchange.Key;
                }
                else
                {// Other (known or unknown)
                }

                var sourceRelayId = MemoryMarshal.Read<ushort>(source.Span);
                if (sourceRelayId == 0)
                {// RelayId(Source/Destination), RelayHeader, Content, Padding
                    var sourceSpan = source.Owner.ByteArray.AsSpan(RelayHeader.RelayIdLength);
                    span.CopyTo(sourceSpan.Slice(RelayHeader.Length));

                    var contentLength = span.Length;
                    var multiple = contentLength & ~15;
                    var paddingLength = contentLength == multiple ? 0 : (multiple + 16 - contentLength);

                    // RelayHeader
                    var relayHeader = new RelayHeader(RandomVault.Crypto.NextUInt32(), (byte)paddingLength, new(endpoint));//
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

                aes.Key = exchange.Key;
                if (!aes.TryEncryptCbc(span, exchange.Iv, span, out _, PaddingMode.None))
                {
                    goto Exit;
                }

                MemoryMarshal.Write(source.Span, exchange.RelayId); // SourceRelayId
                MemoryMarshal.Write(source.Span.Slice(sizeof(ushort)), exchange.Endpoint.RelayId); // DestinationRelayId
                source.IncrementAndShare();
                this.netTerminal.NetSender.Send_NotThreadSafe(exchange.Endpoint.EndPoint, source);//
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
