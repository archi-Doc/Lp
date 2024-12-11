﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Netsphere.Packet;

namespace Netsphere.Relay;

internal class RelayKey
{
    public RelayKey()
    {
    }

    public RelayKey(RelayNode.GoshujinClass relayNodes)
    {// using (relayNodes.LockObject.EnterScope())
        this.NumberOfRelays = relayNodes.Count;
        var node = relayNodes.LinkedListChain.First;
        if (node is not null)
        {
            this.FirstEndpoint = node.Endpoint;

            this.EmbryoKeyArray = new byte[relayNodes.Count][];
            this.EmbryoSaltArray = new ulong[relayNodes.Count];
            this.EmbryoSecretArray = new ulong[relayNodes.Count];
            for (var i = 0; node is not null; i++)
            {
                this.EmbryoKeyArray[i] = node.EmbryoKey;
                this.EmbryoSaltArray[i] = node.EmbryoSalt;
                this.EmbryoSecretArray[i] = node.EmbryoSecret;

                node = node.LinkedListLink.Next;
            }
        }
    }

    public int NumberOfRelays { get; }

    public NetEndpoint FirstEndpoint { get; }

    public byte[][] EmbryoKeyArray { get; } = [];

    public ulong[] EmbryoSaltArray { get; } = [];

    public ulong[] EmbryoSecretArray { get; } = [];

    public bool TryDecrypt(NetEndpoint endpoint, ref BytePool.RentMemory rentMemory, out NetAddress originalAddress, out int relayNumber)
    {
        relayNumber = 0;
        if (!endpoint.Equals(this.FirstEndpoint))
        {
            originalAddress = default;
            return false;
        }

        var span = rentMemory.Span;
        if (span.Length < (RelayHeader.RelayIdLength + RelayHeader.Length))
        {
            goto Exit;
        }

        span = span.Slice(RelayHeader.RelayIdLength);
        var salt4 = MemoryMarshal.Read<uint>(span);
        var encryptedSpan = span.Slice(RelayHeader.PlainLength);
        Span<byte> nonce32 = stackalloc byte[32];

        for (var i = 0; i < this.NumberOfRelays; i++)
        {
            if (rentMemory.RentArray is null)
            {
                goto Exit;
            }

            RelayHelper.CreateNonce(salt4, this.EmbryoSaltArray[i], this.EmbryoSecretArray[i], nonce32);
            Aegis256.TryDecrypt(encryptedSpan, encryptedSpan, nonce32, this.EmbryoKeyArray[i], default, 0);

            var relayHeader = MemoryMarshal.Read<RelayHeader>(span);
            if (relayHeader.Zero == 0)
            {// Decrypted
                var span2 = rentMemory.RentArray.AsSpan();
                MemoryMarshal.Write(span2, relayHeader.NetAddress.RelayId);
                span2 = span2.Slice(sizeof(ushort));
                MemoryMarshal.Write(span2, (ushort)0);
                span2 = span2.Slice(sizeof(ushort));

                span = span.Slice(RelayHeader.Length);
                span.CopyTo(span2);
                rentMemory = rentMemory.RentArray.AsMemory(0, RelayHeader.RelayIdLength + span.Length);

                originalAddress = relayHeader.NetAddress;
                relayNumber = i + 1;
                return true;
            }
        }

        goto Exit; // It might not be encrypted.

Exit:
        originalAddress = default;
        return false;
    }

    public bool TryEncrypt(int relayNumber, NetAddress destination, ReadOnlySpan<byte> content, out BytePool.RentMemory encrypted, out NetEndpoint relayEndpoint)
    {
        Debug.Assert(content.Length >= 4);
        Debug.Assert(content[0] == 0);
        Debug.Assert(content[1] == 0);

        // PacketHeaderCode
        content = content.Slice(4); // Skip relay id

        if (relayNumber < 0)
        {// The target relay
            if (this.NumberOfRelays < -relayNumber)
            {
                goto Error;
            }

            relayNumber = -relayNumber;
        }
        else if (relayNumber > 0)
        {// The minimum number of relays
            if (this.NumberOfRelays < relayNumber)
            {
                goto Error;
            }

            relayNumber = this.NumberOfRelays;
        }
        else
        {// No relay
            goto Error;
        }

        encrypted = PacketPool.Rent().AsMemory();

        // RelayId
        var span = encrypted.Span;
        BitConverter.TryWriteBytes(span, (RelayId)0); // SourceRelayId
        span = span.Slice(sizeof(RelayId));
        BitConverter.TryWriteBytes(span, this.FirstEndpoint.RelayId); // DestinationRelayId
        span = span.Slice(sizeof(RelayId));

        // RelayHeader
        var relayHeader = new RelayHeader(RandomVault.Default.NextUInt32(), destination);
        MemoryMarshal.Write(span, relayHeader);
        span = span.Slice(RelayHeader.Length);

        // Content
        content.CopyTo(span);
        span = span.Slice(content.Length);

        var encryptionContent = encrypted.Span.Slice(RelayHeader.RelayIdLength + RelayHeader.PlainLength, RelayHeader.CipherLength + content.Length);
        Span<byte> nonce32 = stackalloc byte[32];
        try
        {
            for (var i = relayNumber - 1; i >= 0; i--)
            {
                RelayHelper.CreateNonce(relayHeader.Salt, this.EmbryoSaltArray[i], this.EmbryoSecretArray[i], nonce32);
                Aegis256.Encrypt(encryptionContent, encryptionContent, nonce32, this.EmbryoKeyArray[i], default, 0);
            }
        }
        catch
        {
            goto Error;
        }

        encrypted = encrypted.Slice(0, RelayHeader.RelayIdLength + RelayHeader.Length + content.Length);
        Debug.Assert(encrypted.Memory.Length <= NetConstants.MaxPacketLength);
        relayEndpoint = this.FirstEndpoint;
        return true;

Error:
        encrypted = default;
        relayEndpoint = default;
        return false;
    }
}
