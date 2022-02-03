// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

#pragma warning disable SA1401 // Fields should be private

namespace ZenItz;

public interface IItzShip<T>
    where T : struct
{
    void Set(Identifier primaryId, Identifier? secondaryId, ref T value);

    ItzResult Get(Identifier primaryId, Identifier? secondaryId, out T value);
}

public partial class ItzShip<T> : IItzShip<T>
    where T : struct
{
    [TinyhandObject]
    [ValueLinkObject]
    private sealed partial class Item
    {
        [Link(Primary = true, Name = "Queue", Type = ChainType.QueueList)]
        public Item(PrimarySecondaryIdentifier key, T value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Item()
        {
        }

        [Key(0)]
        internal T Value;

        [Key(1)]
        [Link(Type = ChainType.Unordered)]
        internal PrimarySecondaryIdentifier Key;
    }

    public ItzShip(int maxCapacity)
    {
        this.MaxCapacity = maxCapacity;
    }

    public void Set(Identifier primaryId, Identifier? secondaryId, ref T value)
    {
        var key = new PrimarySecondaryIdentifier(primaryId, secondaryId);
        lock (this.goshujin)
        {
            if (this.goshujin.KeyChain.TryGetValue(key, out var item))
            {// Update
                item.Value = value;
                this.goshujin.QueueChain.Remove(item);
                this.goshujin.QueueChain.Enqueue(item);
            }
            else
            {// New
                item = new Item(key, value);
                this.goshujin.Add(item);

                if (this.goshujin.QueueChain.Count > this.MaxCapacity)
                {// Remove the oldest item;
                    this.goshujin.QueueChain.Dequeue().Goshujin = null;
                }
            }
        }
    }

    public ItzResult Get(Identifier primaryId, Identifier? secondaryId, out T value)
    {
        var key = new PrimarySecondaryIdentifier(primaryId, secondaryId);
        lock (this.goshujin)
        {
            if (this.goshujin.KeyChain.TryGetValue(key, out var item))
            {// Get
                value = item.Value;
                return ItzResult.Success;
            }
        }

        value = default;
        return ItzResult.NoData;
    }

    public int MaxCapacity { get; }

    private Item.GoshujinClass goshujin = new();
    private ConcurrentDictionary<PrimarySecondaryIdentifier, Item> concurrentDictionary = new();
}
