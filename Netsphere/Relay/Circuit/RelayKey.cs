// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Netsphere.Relay;

internal class RelayKey
{
    private const int AesPoolSize = 32;
    private static readonly ObjectPool<Aes> AesPool = new(
        () =>
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            // aes.Mode = CipherMode.CBC;
            // aes.Padding = PaddingMode.PKCS7;
            return aes;
        },
        AesPoolSize);

    public RelayKey()
    {
    }

    public RelayKey(RelayNode.GoshujinClass relayNodes)
    {// lock (relayNodes.SyncObject)
        this.NumberOfRelays = relayNodes.Count;
        var node = relayNodes.LinkedListChain.First;
        if (node is not null)
        {
            this.FirstEndpoint = node.Endpoint;

            this.KeyArray = new byte[relayNodes.Count][];
            this.IvArray = new byte[relayNodes.Count][];
            for (var i = 0; node is not null; i++)
            {
                this.KeyArray[i] = node.Key;
                this.IvArray[i] = node.Iv;
                node = node.LinkedListLink.Next;
            }
        }
    }

    public int NumberOfRelays { get; }

    public NetEndpoint FirstEndpoint { get; }

    public byte[][] KeyArray { get; } = [];

    public byte[][] IvArray { get; } = [];

    public bool TryDecrypt(NetEndpoint endpoint, ref BytePool.RentMemory rentMemory, out NetAddress originalAddress)
    {
        if (!endpoint.Equals(this.FirstEndpoint))
        {
            originalAddress = default;
            return false;
        }

        var span = rentMemory.Span;
        if (span.Length < RelayHeader.Length)
        {
            goto Exit;
        }

        span = span.Slice(RelayHeader.RelayIdLength);
        if ((span.Length & 15) != 0)
        {// It might not be encrypted.
            goto Exit;
        }

        var aes = AesPool.Get();

        try
        {
            for (var i = 0; i < this.NumberOfRelays; i++)
            {
                if (rentMemory.RentArray is null)
                {
                    goto Exit;
                }

                aes.Key = this.KeyArray[i];
                aes.TryDecryptCbc(span, this.IvArray[i], span, out _, PaddingMode.None);

                var relayHeader = MemoryMarshal.Read<RelayHeader>(span);
                if (relayHeader.Zero == 0)
                {// Decrypted
                    var span2 = rentMemory.RentArray.AsSpan();
                    MemoryMarshal.Write(span2, relayHeader.NetAddress.RelayId);
                    span2 = span2.Slice(sizeof(ushort));
                    MemoryMarshal.Write(span2, (ushort)0);
                    span2 = span2.Slice(sizeof(ushort));

                    span = span.Slice(RelayHeader.Length);
                    var contentLength = span.Length - relayHeader.PaddingLength;
                    span.Slice(0, contentLength).CopyTo(span2);
                    rentMemory = rentMemory.RentArray.AsMemory(0, RelayHeader.RelayIdLength + contentLength);

                    originalAddress = relayHeader.NetAddress;
                    return true;
                }
            }

            goto Exit; // It might not be encrypted.
        }
        catch
        {
        }
        finally
        {
            AesPool.Return(aes);
        }

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
        var multiple = content.Length & ~15;
        var paddingLength = content.Length == multiple ? 0 : (multiple + 16 - content.Length);

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

        var aes = AesPool.Get();
        encrypted = PacketPool.Rent().AsMemory();

        // RelayId
        var span = encrypted.Span;
        BitConverter.TryWriteBytes(span, (ushort)0); // SourceRelayId
        span = span.Slice(sizeof(ushort));
        BitConverter.TryWriteBytes(span, this.FirstEndpoint.RelayId); // DestinationRelayId
        span = span.Slice(sizeof(ushort));

        // RelayHeader
        var relayHeader = new RelayHeader(RandomVault.Crypto.NextUInt32(), (byte)paddingLength, destination);
        MemoryMarshal.Write(span, relayHeader);
        span = span.Slice(RelayHeader.Length);

        // Content
        content.CopyTo(span);
        span = span.Slice(content.Length);
        span.Slice(0, paddingLength).Fill(0x07);

        var headerAndContentLength = RelayHeader.Length + content.Length + paddingLength;
        var headerAndContent = encrypted.Span.Slice(RelayHeader.RelayIdLength, headerAndContentLength);
        try
        {
            for (var i = relayNumber - 1; i >= 0; i--)
            {
                aes.Key = this.KeyArray[i];
                aes.TryEncryptCbc(headerAndContent, this.IvArray[i], headerAndContent, out _, PaddingMode.None);
            }
        }
        catch
        {
            goto Error;
        }

        AesPool.Return(aes);

        encrypted = encrypted.Slice(0, RelayHeader.RelayIdLength + headerAndContentLength);
        Debug.Assert(encrypted.Memory.Length <= NetConstants.MaxPacketLength);
        relayEndpoint = this.FirstEndpoint;
        return true;

Error:
        encrypted = default;
        relayEndpoint = default;
        return false;
    }
}
