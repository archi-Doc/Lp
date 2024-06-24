// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Stats;

[TinyhandObject(UseServiceProvider = true)]
public partial class PublicAccess
{
    private const int PortArrayLength = 256;
    private const int PortArrayMask = PortArrayLength - 1;

    public enum Type
    {
        Unknown,
        Direct,
        Concrete,
        Cone,
        Symmetric,
    }

    [ValueLinkObject]
    private partial class PortCounter
    {
        public PortCounter()
        {
        }

        [Link(Primary = true, Type = ChainType.Unordered, AddValue = false)]
        public int Port { get; internal set; }

        [Link(Type = ChainType.Ordered)]
        public int Counter { get; private set; }

        public int Increment()
            => ++this.CounterValue;

        public int Decrement()
            => --this.CounterValue;
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
    private ObjectPool<PortCounter> portCounterPool = new(() => new(), PortArrayLength);
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

        lock (this.syncObject)
        {
            PortCounter? counter;
            var originalValue = this.portArray[this.portIndex];
            if (originalValue != 0)
            {// Decrement
                if (this.portCounters.PortChain.TryGetValue(originalValue, out counter))
                {
                    if (counter.Decrement() == 0)
                    {
                        counter.Goshujin = default;
                        this.portCounterPool.Return(counter);
                    }
                }
            }

            this.portArray[this.portIndex] = port;
            if (!this.portCounters.PortChain.TryGetValue(port, out counter))
            {
                counter = this.portCounterPool.Get();
                counter.Port = port;
                counter.Goshujin = this.portCounters;
            }

            counter.Increment();

            this.portMode = this.portCounters.CounterChain.Last!.Port;
            this.portIndex = (this.portIndex + 1) & PortArrayMask;
        }

        Console.WriteLine($"{port} {this.portMode}");//
    }
}
