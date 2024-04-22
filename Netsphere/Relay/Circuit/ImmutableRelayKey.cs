// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    public bool TryEncrypt(int relayNumber, ref ByteArrayPool.MemoryOwner owner, out NetEndpoint relayEndpoint)
    {
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
        var packet = PacketPool.Rent();

        // RelayId
        var span = packet.ByteArray.AsSpan();
        BitConverter.TryWriteBytes(span, this.FirstEndpoint.RelayId);
        var span2 = span.Slice(sizeof(ushort));

        // RelayHeader
        BitConverter.TryWriteBytes(span2, 0u); // Zero
        span2 = span2.Slice(sizeof(uint));
        var salt = RandomVault.Crypto.NextUInt32();
        BitConverter.TryWriteBytes(span2, salt); // Salt
        span2 = span2.Slice(sizeof(uint));

        for (var i = 0; i < relayNumber; i++)
        {

        }

        packet.Return();
        AesPool.Return(aes);

Error:
        relayEndpoint = default;
        return false;
    }
}
