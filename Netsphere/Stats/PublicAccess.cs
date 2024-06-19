// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true)]
public partial class PublicAccess
{
    private const int PortArrayLength = 32;

    public enum Type
    {
        Unknown,
        Direct,
        Cone,
        Symmetric,
    }

    [ValueLinkObject]
    private partial class PortCounter
    {
        public PortCounter(int port)
        {
            this.Port = port;
        }

        [Link(Primary = true, Type = ChainType.Unordered, AddValue = false)]
        public int Port { get; private set; }

        [Link(Type = ChainType.Ordered)]
        public int Counter { get; private set; }

        public void Increment()
        {
            this.CounterValue++;
        }

        public void Decrement()
        {
            this.CounterValue--;
            if (this.Counter == 0)
            {
                this.Goshujin = default;
            }
        }
    }

    public PublicAccess(NetBase netBase)
    {
        this.netBase = netBase;
    }

    #region FieldAndProperty

    public Type AccessType { get; private set; }

    private readonly NetBase netBase;

    private object syncObject = new();
    private PortCounter.GoshujinClass portCounters = new();
    private int portIndex;
    private int[] portArray = new int[PortArrayLength];
    private int portMode;

    #endregion

    public void ReportPortNumber(int port)
    {
        if (port == 0)
        {
            return;
        }

        Console.WriteLine(port);
        lock (this.syncObject)
        {
            PortCounter? counter;
            var originalValue = this.portArray[this.portIndex];
            if (originalValue != 0)
            {// Decrement
                if (this.portCounters.PortChain.TryGetValue(originalValue, out counter))
                {
                    counter.Decrement();
                }
            }

            this.portArray[this.portIndex] = port;
            if (!this.portCounters.PortChain.TryGetValue(port, out counter))
            {
                counter = new(port);
                counter.Goshujin = this.portCounters;
            }

            counter.Increment();

            this.portMode = this.portCounters.CounterChain.Last!.Counter;
            if (this.portIndex < PortArrayLength - 1)
            {
                this.portIndex++;
            }
            else
            {
                this.portIndex = 0;
            }
        }
    }
}
