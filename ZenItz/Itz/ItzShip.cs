// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IItzShip<T>
    where T : struct
{
    void Set(Identifier primaryId, Identifier secondaryId, ref T value);

    ItzResult Get(Identifier primaryId, Identifier secondaryId, out T value);
}

public partial class ItzShip<T> : IItzShip<T>
    where T : struct
{
    [ValueLinkObject]
    private sealed partial class Item
    {
        public Item(Identifier primaryId)
        {
            this.PrimaryId = primaryId;
        }

        [Link(Type = ChainType.Unordered)]
        public Identifier PrimaryId { get; set; }
    }

    public ItzShip()
    {
    }

    public void Set(Identifier primaryId, Identifier secondaryId, ref T value)
    {
    }

    public ItzResult Get(Identifier primaryId, Identifier secondaryId, out T value)
    {
        value = default;
        return ItzResult.Success;
    }

    private Item.GoshujinClass goshujin = new();
}
