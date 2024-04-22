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
        public enum Type
        {
            Inner,
            Outer,
        }

        public Item(ushort relayId, NetEndpoint endpoint)
        {
            this.RelayId = relayId;
            this.Endpoint = endpoint;
        }

        public Type RelayType { get; private set; }

        [Link(Primary = true, Type = ChainType.Unordered, AddValue = false)]
        public ushort RelayId { get; private set; }

        [Link(Type = ChainType.Unordered, AddValue = false)]
        public NetEndpoint Endpoint { get; private set; }

        public long RelayPoint { get; private set; }

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

    public RelayAgent(IRelayControl relayControl)
    {
        this.relayControl = relayControl;
    }

    public RelayResult Add(ushort relayId, NetEndpoint innerEndpoint)
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

            this.items.Add(new(relayId, innerEndpoint));
        }

        return RelayResult.Success;
    }

    public bool ProcessReceive(NetEndpoint endpoint, out ByteArrayPool.MemoryOwner decrypted)
    {
        Item? item;
        lock (this.items.SyncObject)
        {
            item = this.items.RelayIdChain.FindFirst(endpoint.RelayId);
            if (item is null || !item.DecrementAndCheck())
            {
                goto Exit;
            }
        }

        if (item.RelayType == Item.Type.Inner)
        {// Inner -> Outer
            if (item.Endpoint.EndPointEquals(endpoint))
            {// Inner -> Outer: Decrypt
                // Decrypted
                if (netAddress == NetAddress.Relay)
                {
                }
                else
                {
                }

                // Not decrypted
                if (item.OuterRelay is { } outerRelay)
                {

                }
                else
                {// Discard
                }
            }
            else if (item.OuterRelay is null)
            {// Unknown

            }
        }
        else
        {// Outer -> Inner
            if (item.Endpoint.EndPointEquals(endpoint))
            {// Outer -> Inner: Encrypt
            }
            else
            {// Unknown: Discard
            }
        }

Exit:
        decrypted = default;
        return false;
    }

    private readonly IRelayControl relayControl;
    private readonly Item.GoshujinClass items = new();
}
