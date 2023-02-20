// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
internal partial class MemoryStat
{
    private const int MaxItem = 1_000;

    public MemoryStat()
    {
    }

    public void Add(int size)
    {
        Item? item = null;
        while (this.items.Count >= MaxItem)
        {
            item = this.items.QueueChain.Peek();
            item.Goshujin = null;
            this.totalSize -= item.Size;
        }

        item ??= new Item();
        item.Size = size;
        item.Goshujin = this.items;
        this.totalSize += size;
    }

    public int AverageSize
    {
        get
        {
            if (this.items.Count == 0)
            {
                return 0;
            }

            return (int)(this.totalSize / this.items.Count);
        }
    }

    [Key(0)]
    private long totalSize;

    [Key(1)]
    private Item.GoshujinClass items = default!;

    [TinyhandObject]
    [ValueLinkObject]
    internal partial class Item
    {
        [Link(Primary = true, Type = ChainType.QueueList, Name = "Queue")]
        public Item()
        {
        }

        [Key(0)]
        public int Size { get; set; }

        public override string ToString() => this.Size.ToString();
    }
}
