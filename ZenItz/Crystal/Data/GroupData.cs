// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial class GroupData : BaseData
{
    internal GroupData()
    {
    }

    public void Add(BaseData data)
    {
        this.children ??= new();
        this.children.Add(data);
    }

    [Key(3)]
    private List<BaseData>? children;

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
