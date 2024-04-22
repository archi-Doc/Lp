// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using Arc.Collections;

namespace Netsphere.Relay;

internal class ImmutableRelayKey
{
    private static readonly ObjectPool<Aes> AesPool = new(
        () =>
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            // aes.Mode = CipherMode.CBC;
            // aes.Padding = PaddingMode.PKCS7;
            return aes;
        },
        32);

    public ImmutableRelayKey()
    {
    }

    public ImmutableRelayKey(RelayNode.GoshujinClass relayNodes)
    {// lock (relayNodes.SyncObject)
        this.NumberOfRelays = relayNodes.Count;
        if (relayNodes.Count > 0)
        {
            var chain = relayNodes.ListChain;
            this.FirstRelay = new(chain[0].NetNode, chain[0].RelayId);

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

    public NetEndpoint FirstRelay { get; }

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

        AesPool.Return(aes);

Error:
        relayEndpoint = default;
        return false;
    }
}
