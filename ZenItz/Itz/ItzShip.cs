// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1401 // Fields should be private

namespace ZenItz;

public partial class ItzShip<T> : IItzShip<T>
    where T : IItzPayload
{
    [TinyhandObject]
    [ValueLinkObject]
    private sealed partial class Item
    {
        [Link(Primary = true, Name = "Queue", Type = ChainType.QueueList)]
        public Item(Identifier key, T value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Item()
        {
        }

        [Key(0)]
        [Link(Type = ChainType.Unordered)]
        internal Identifier Key;

        [Key(1)]
        internal T Value = default!;
    }

    public ItzShip(int maxCapacity)
    {
        this.MaxCapacity = maxCapacity;
    }

    public void Set(in Identifier id, in T value)
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

                if (this.goshujin.QueueChain.Count > this.MaxCapacity)
                {// Remove the oldest item;
                    this.goshujin.QueueChain.Dequeue().Goshujin = null;
                }
            }
        }
    }

    public ItzResult Get(in Identifier id, out T value)
    {
        lock (this.goshujin)
        {
            if (this.goshujin.KeyChain.TryGetValue(id, out var item))
            {// Get
                value = item.Value;
                return ItzResult.Success;
            }
        }

        value = default!;
        return ItzResult.NoData;
    }

    public void Serialize(ref Tinyhand.IO.TinyhandWriter writer)
    {
        lock (this.goshujin)
        {
            TinyhandSerializer.Serialize(ref writer, this.goshujin);
        }
    }

    public int Count()
    {
        lock (this.goshujin)
        {
            return this.goshujin.QueueChain.Count;
        }
    }

    public bool Deserialize(ReadOnlySpan<byte> span, out int bytesRead)
    {
        lock (this.goshujin)
        {
            if (!TinyhandSerializer.TryDeserialize<Item.GoshujinClass>(span, out var newGoshujin, out bytesRead))
            {
                return false;
            }

            this.goshujin = newGoshujin;
            return true;
        }
    }

    public int MaxCapacity { get; }

    private Item.GoshujinClass goshujin = new();
}
