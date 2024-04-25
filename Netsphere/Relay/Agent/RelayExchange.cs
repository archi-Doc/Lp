// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[ValueLinkObject]
internal partial class RelayExchange
{
    public RelayExchange(ushort relayId, ushort outerRelayId, ServerConnection serverConnection)
    {
        this.RelayId = relayId;
        this.OuterRelayId = outerRelayId;
        this.Endpoint = serverConnection.DestinationEndpoint;

        this.Key = new byte[Connection.EmbryoKeyLength];
        serverConnection.UnsafeCopyKey(this.Key);
        this.Iv = new byte[Connection.EmbryoIvLength];
        serverConnection.UnsafeCopyIv(this.Iv);
    }

    [Link(Primary = true, Type = ChainType.Unordered, AddValue = false)]
    public ushort RelayId { get; private set; }

    [Link(UnsafeTargetChain = "RelayIdChain")]
    public ushort OuterRelayId { get; private set; }

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
