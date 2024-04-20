// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

/// <summary>
/// Manages relays and conducts the actual relay processing on the server side.
/// </summary>
public partial class RelayAgent
{
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private partial class Item
    {
        public Item(ushort relayId, NetNode netNode)
        {
            this.RelayId = relayId;
            this.NetNode = netNode;
        }

        [Link(Primary = true, Type = ChainType.Unordered)]
        public ushort RelayId { get; private set; }

        [Link(Type = ChainType.Unordered)]
        public NetNode NetNode { get; private set; }
    }

    public RelayAgent(IRelayControl relayControl)
    {
        this.relayControl = relayControl;
    }

    public RelayResult Add(ushort relayId, NetNode innerNode)
    {
        lock (this.items.SyncObject)
        {
            if (this.items.Count > this.relayControl.MaxParallelRelays)
            {
                return RelayResult.ParallelRelayLimit;
            }

            if (this.items.RelayIdChain.ContainsKey(relayId))
            {
                return RelayResult.DuplicateRelayId;
            }

            this.items.Add(new(relayId, innerNode));
        }

        return RelayResult.Success;
    }

    private readonly IRelayControl relayControl;
    private readonly Item.GoshujinClass items = new();
}
