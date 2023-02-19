// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial class GroupData : BaseData
{
    internal GroupData()
    {
    }

    public void AddChild(BaseData data)
    {
        using (this.semaphore.Lock())
        {
            data.Initialize(this.Crystal, this, false);
            this.children.Add(data);
        }
    }

    public void DeleteChild(BaseData data)
    {
        using (this.semaphore.Lock())
        {
            this.children.Remove(data);
            data.Delete();
        }
    }

    [Key(3)]
    private List<BaseData> children = new();

    protected override IEnumerator<BaseData> EnumerateInternal()
        => this.children.GetEnumerator();

    protected override void DeleteInternal()
    {
        this.children.Clear();
    }
}
