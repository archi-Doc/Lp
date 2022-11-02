// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;

namespace Netsphere;

public partial class NetStatus
{
    private const int QueueNodeAddressLimit = 20;

    [ValueLinkObject]
    private partial class QueueNodeAddress
    {
        [Link(Type = ChainType.QueueList, Name = "Queue", Primary = true)]
        public QueueNodeAddress(NodeAddress nodeAddress)
        {
            this.NodeAddress = nodeAddress;
        }

        // [Link(Type = ChainType.Unordered)]
        public NodeAddress NodeAddress { get; private set; }
    }

    [ValueLinkObject]
    private partial class CountNodeAddress
    {
        public CountNodeAddress(NodeAddress nodeAddress)
        {
            this.Count = 1;
            this.NodeAddress = nodeAddress;
        }

        public void Increment()
        {
            this.CountValue++;
        }

        public void Decrement()
        {
            this.CountValue--;
            if (this.CountValue == 0)
            {
                this.Goshujin = null;
            }
        }

        [Link(Type = ChainType.Ordered)]
        public int Count { get; private set; }

        [Link(Type = ChainType.Unordered)]
        public NodeAddress NodeAddress { get; private set; }
    }

    public NetStatus(NetBase netBase)
    {
        this.NetBase = netBase;
    }

    public NodeInformation GetMyNodeInformation(bool isAlternative)
    {
        if (isAlternative)
        {
            return this.AlternativeNodeInformation;
        }
        else
        {
            return this.MyNodeInformation;
        }
    }

    public void ReportMyNodeAddress(NodeAddress nodeAddress)
    {
        lock (this.queueGoshujin)
        {
            QueueNodeAddress? queue;
            CountNodeAddress? count;

            while (this.queueGoshujin.Count >= QueueNodeAddressLimit)
            {// Remove
                queue = this.queueGoshujin.QueueChain.Peek();
                queue.Goshujin = null;

                if (this.countGoshujin.NodeAddressChain.TryGetValue(queue.NodeAddress, out count))
                {
                    count.Decrement();
                }
            }

            queue = new QueueNodeAddress(nodeAddress);
            queue.Goshujin = this.queueGoshujin;
            if (this.countGoshujin.NodeAddressChain.TryGetValue(queue.NodeAddress, out count))
            {
                count.Increment();
            }
            else
            {
                count = new(nodeAddress);
                count.Goshujin = this.countGoshujin;
            }

            if (this.countGoshujin.CountChain.Last is { } last)
            {
                var myNodeAddress = last.NodeAddress;
                this.MyNodeInformation.SetAddress(myNodeAddress.Address);
            }
        }
    }

    public NetBase NetBase { get; }

    public NodeInformation MyNodeInformation
    {
        get
        {
            if (this.myNodeInformation == null)
            {
                this.myNodeInformation = new(new NodeAddress(IPAddress.None, (ushort)this.NetBase.NetsphereOptions.Port));
                this.myNodeInformation.PublicKey = this.NetBase.NodePublicKey;
            }

            return this.myNodeInformation;
        }
    }

    public NodeInformation AlternativeNodeInformation
    {
        get
        {
            if (this.alternativeNodeInformation == null)
            {
                this.alternativeNodeInformation = new(new NodeAddress(IPAddress.None, 0));
                this.alternativeNodeInformation.PublicKey = NodePrivateKey.AlternativePrivateKey.ToPublicKey();
            }

            return this.alternativeNodeInformation;
        }
    }

    private NodeInformation? myNodeInformation;
    private NodeInformation? alternativeNodeInformation;
    private QueueNodeAddress.GoshujinClass queueGoshujin = new();
    private CountNodeAddress.GoshujinClass countGoshujin = new();
}
