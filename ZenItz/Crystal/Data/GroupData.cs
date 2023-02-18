// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BigMachines;

namespace CrystalData;

[TinyhandObject]
[ValueLinkObject]
public partial class GroupData : BaseData
{
    [Link(Primary = true, Name = "List", Type = ChainType.LinkedList)]
    internal GroupData()
    {
    }

    public void Add(BaseData data)
    {
        this.children ??= new();
        this.children.Add();
        this.children.Add(data);
    }

    [Key(3)]
    private GoshujinClass? children;

    protected override IEnumerator<BaseData> EnumerateInternal()
    {
        if (this.children == null)
        {
            yield break;
        }

        foreach (var x in this.children)
        {
            yield return x;
        }
    }

    protected override void DeleteInternal()
    {
        if (this.Parent != null)
        {
            this.Parent
        }
        this.children = null;
    }
}
