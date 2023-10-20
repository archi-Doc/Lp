// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401 // Fields should be private

using System;
using Tinyhand.IO;

namespace CrystalData;

public partial class Mono<TIdentifier>
{
    public partial class StandardGroup<TMonoData> : IMonoGroup<TMonoData>
        where TMonoData : IMonoData, ITinyhandSerialize<TMonoData>
    {
        [TinyhandObject]
        [ValueLinkObject]
        private sealed partial class Item
        {
            [Link(Primary = true, Name = "Queue", Type = ChainType.QueueList)]
            public Item(TIdentifier key, TMonoData value)
            {
                this.Key = key;
                this.Value = value;
            }

            public Item()
            {
            }

            [Key(0)]
            [Link(Type = ChainType.Unordered)]
            internal TIdentifier Key = default!;

            [Key(1)]
            internal TMonoData Value = default!;
        }

        public StandardGroup(int capacity)
        {
            this.Capacity = capacity;
        }

        public void Set(in TIdentifier id, in TMonoData value)
        {
            lock (this.goshujin)
            {
                if (this.goshujin.KeyChain.TryGetValue(id, out var item))
                {// Update
                    item.Value = value;
                    this.goshujin.QueueChain.Remove(item);
                    this.goshujin.QueueChain.Enqueue(item);
                }
                else
                {// New
                    item = new Item(id, value);
                    this.goshujin.Add(item);

                    if (this.goshujin.QueueChain.Count > this.Capacity)
                    {// Remove the oldest item;
                        this.goshujin.QueueChain.Dequeue().Goshujin = null;
                    }
                }
            }
        }

        public bool TryGet(in TIdentifier id, out TMonoData value)
        {
            lock (this.goshujin)
            {
                if (this.goshujin.KeyChain.TryGetValue(id, out var item))
                {// Get
                    value = item.Value;
                    return true;
                }
            }

            value = default!;
            return false;
        }

        public bool Remove(in TIdentifier id)
        {
            lock (this.goshujin)
            {
                if (this.goshujin.KeyChain.TryGetValue(id, out var item))
                {
                    item.Goshujin = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void SetCapacity(int capacity)
        {
            this.Capacity = capacity;
        }

        public int Count()
        {
            lock (this.goshujin)
            {
                return this.goshujin.QueueChain.Count;
            }
        }

        void ITinyhandSerialize.Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)
        {
            lock (this.goshujin)
            {
                TinyhandSerializer.Serialize(ref writer, this.goshujin, options);
            }
        }

        void ITinyhandSerialize.Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
        {
            lock (this.goshujin)
            {
                Item.GoshujinClass? g = default;
                try
                {
                    g = TinyhandSerializer.Deserialize<Item.GoshujinClass>(ref reader, options);
                }
                catch
                {
                }

                if (g is not null)
                {
                    this.goshujin = g;
                }
            }
        }

        public int Capacity { get; private set; }

        private Item.GoshujinClass goshujin = new();
    }
}
