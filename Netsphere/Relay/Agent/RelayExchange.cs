// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
internal partial class RelayExchange
{
    [Link(Primary = true, Name = "LinkedList", Type = ChainType.LinkedList)]
    public RelayExchange(IRelayControl relayControl, ushort relayId, ushort outerRelayId, ServerConnection serverConnection)
    {
        this.RelayId = relayId;
        this.OuterRelayId = outerRelayId;
        this.Endpoint = serverConnection.DestinationEndpoint;
        this.LastAccessMics = Mics.FastSystem;

        this.Key = new byte[Connection.EmbryoKeyLength];
        serverConnection.UnsafeCopyKey(this.Key);
        this.Iv = new byte[Connection.EmbryoIvLength];
        serverConnection.UnsafeCopyIv(this.Iv);

        this.RelayRetensionMics = relayControl.DefaultRelayRetensionMics;
    }

    [Link(Type = ChainType.Unordered, AddValue = false)]
    public ushort RelayId { get; private set; }

    [Link(UnsafeTargetChain = "RelayIdChain")]
    public ushort OuterRelayId { get; private set; }

    public NetEndpoint Endpoint { get; private set; }

    public NetEndpoint OuterEndpoint { get; set; }

    public long RelayPoint { get; internal set; }

    public long LastAccessMics { get; private set; }

    public long RelayRetensionMics { get; private set; }

    internal byte[] Key { get; private set; }

    internal byte[] Iv { get; private set; }

    public bool DecrementAndCheck()
    {// lock (items)
        if (this.RelayPoint-- <= 0)
        {// All RelayPoints have been exhausted.
            this.Clean();
            this.Goshujin = null;
            return false;
        }
        else
        {
            this.LastAccessMics = Mics.FastSystem;
            return true;
        }
    }

    public void Clean()
    {
    }
}
