// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

internal class ImmutableRelayKey
{
    public ImmutableRelayKey()
    {
        this.KeyArray = [];
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

    public byte[][] KeyArray { get; }

    public byte[][] IvArray { get; }
}
