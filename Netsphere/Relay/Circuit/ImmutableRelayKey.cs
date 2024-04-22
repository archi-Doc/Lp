// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Arc.Collections;

namespace Netsphere.Relay;

internal class ImmutableRelayKey
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

    public ImmutableRelayKey()
    {
    }

    public ImmutableRelayKey(RelayNode.GoshujinClass relayNodes)
    {// lock (relayNodes.SyncObject)
        this.NumberOfRelays = relayNodes.Count;
        if (relayNodes.Count > 0)
        {
            var chain = relayNodes.ListChain;
            this.FirstEndpoint = chain[0].Endpoint;

            this.KeyArray = new byte[relayNodes.Count][];
            this.IvArray = new byte[relayNodes.Count][];
            for (var i = 0; i < relayNodes.Count; i++)
            {
                this.KeyArray[i] = chain[i].Key;
                this.IvArray[i] = chain[i].Iv;
            }
        }
    }

    public int NumberOfRelays { get; }

    public NetEndpoint FirstEndpoint { get; }

    public byte[][] KeyArray { get; } = [];

    public byte[][] IvArray { get; } = [];

    public bool TryEncrypt(int relayNumber, NetAddress destination, ReadOnlySpan<byte> content, out ByteArrayPool.MemoryOwner encrypted, out NetEndpoint relayEndpoint)
    {
        Debug.Assert(content.Length <= (NetConstants.MaxPacketLength - NetConstants.RelayLength));

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
        encrypted = PacketPool.Rent().ToMemoryOwner();

        // RelayId
        var span = encrypted.Span;
        BitConverter.TryWriteBytes(span, this.FirstEndpoint.RelayId);
        span = span.Slice(sizeof(ushort));

        // RelayHeader
        var relayHeader = new RelayHeader(RandomVault.Crypto.NextUInt32(), destination);
        MemoryMarshal.Write(span, relayHeader);
        span = span.Slice(RelayHeader.Length);

        // Content
        content.CopyTo(span);

        var headerAndContentLength = RelayHeader.Length + content.Length;
        var headerAndContent = encrypted.Span.Slice(sizeof(ushort), headerAndContentLength);
        for (var i = 0; i < relayNumber; i++)
        {
            aes.Key = this.KeyArray[i];
            aes.TryEncryptCbc(headerAndContent, this.IvArray[i], headerAndContent, out _, PaddingMode.None);//PaddingMode
        }

        AesPool.Return(aes);

        relayEndpoint = this.FirstEndpoint;
        return true;

Error:
        encrypted = default;
        relayEndpoint = default;
        return false;
    }
}
