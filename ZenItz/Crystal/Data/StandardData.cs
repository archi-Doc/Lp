// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
[ValueLinkObject]
public partial class StandardData : BaseData
{
    [Link(Primary = true, Name = "GetQueue", Type = ChainType.QueueList)]
    internal StandardData()
    {
    }

    public Identifier Identifier => this.identifier;

    [Key(3)]
    [Link(Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    private Identifier identifier = default!;

    [Key(4)]
    private GoshujinClass? childFlakes;

    protected override IEnumerator<BaseData> EnumerateInternal()
    {
        if (this.childFlakes == null)
        {
            yield break;
        }

        foreach (var x in this.childFlakes)
        {
            yield return x;
        }
    }

    protected override void DeleteInternal()
    {
        this.childFlakes = null;
        this.Goshujin = null;
    }

    /*protected override void SaveInternal(bool unload)
    {
        if (this.childFlakes != null)
        {
            foreach (var x in this.childFlakes)
            {
                x.Save(unload);
            }
        }
    }*/
}
