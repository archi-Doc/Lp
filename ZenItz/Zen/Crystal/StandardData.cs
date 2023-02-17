// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;

namespace CrystalData;

[TinyhandObject]
[ValueLinkObject]
public partial class StandardData : BaseData<StandardData.GoshujinClass>
{// LPData
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
            var e = ((IEnumerable<StandardData>)this.childFlakes).GetEnumerator();
            var e2 = (IEnumerable<StandardData>)this.childFlakes;
            foreach (var x in e2)
            {

            }
            foreach (var x in this.childFlakes)
            {
                x.Save(unload);
            }
        }
    }
}
