// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz.Datum;

[TinyhandObject]
[ValueLinkObject]
public partial class Datum : DatumBase<Datum>
{
    [Link(Primary = true, Name = "GetQueue", Type = ChainType.QueueList)]
    internal Datum()
    {
    }

    public Identifier Identifier => this.identifier;

    [Key(3)]
    [Link(Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    private Identifier identifier = default!;

    [Key(4)]
    private Datum.GoshujinClass? childFlakes;

    protected override void DeleteChildren()
    {
        if (this.childFlakes != null)
        {
            foreach (var x in this.childFlakes.ToArray())
            {
                x.DeleteInternal();
            }

            this.childFlakes = null;
        }
    }

    protected override void SaveChildren(bool unload)
    {
        if (this.childFlakes != null)
        {
            foreach (var x in this.childFlakes)
            {
                x.Save(unload);
            }
        }
    }
}
