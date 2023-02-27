// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

using System.Runtime.CompilerServices;
using ValueLink;

namespace LP.Crystal;

[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
public partial class LpData2 : BaseData
{
    public enum LpData2Id
    {
        Deleted = -1,
        Credit = 0,
    }

    public LpData2(ICrystalInternal crystal, BaseData? parent, Identifier identifier)
        : base(crystal, parent)
    {
        this.identifier = identifier;
    }

    [Link(Primary = true, Name = "GetQueue", Type = ChainType.QueueList)]
    public LpData2()
    {
    }

    // public LpDataId GetDataId() => (LpDataId)this.DataId;

    // public void SetDataId(LpDataId dataId) => this.DataId = (int)dataId;

    public new LpData2Id DataId
    {
        get => (LpData2Id)base.DataId;
        set => base.DataId = (int)value;
    }

    public Identifier Identifier => this.identifier;

    [Key(3)]
    [Link(Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    private Identifier identifier = default!;

    [Key(4)]
    private ulong childrenFile;

    private GoshujinClass? children;

    public int Count(LpData2Id id)
    {
        var intId = (int)id;
        var count = 0;
        using (this.semaphore.Lock())
        {
            foreach (var x in this.ChildrenInternal)
            {
                if (x.DataId == intId)
                {
                    count++;
                }
            }
        }

        return count;
    }

    #region Child

    public LockOperation<TData> LockChild<TData>(Identifier id)
        where TData : IDatum
    {
        LpData2? data;
        using (this.semaphore.Lock())
        {
            this.children = this.PrepareChildren();
            if (this.children.IdChain.TryGetValue(id, out data))
            {// Update GetQueue chain
                this.children.GetQueueChain.Remove(data);
                this.children.GetQueueChain.Enqueue(data);
            }
            else
            {
                return default;
            }
        }

        return data.Lock<TData>();
    }

    public LpData2 GetOrCreateChild(Identifier id)
    {
        LpData2? data;
        using (this.semaphore.Lock())
        {
            this.children = this.PrepareChildren();
            if (!this.children.IdChain.TryGetValue(id, out data))
            {
                data = new LpData2(this.Crystal, this, id);
                this.children.Add(data);
            }
            else
            {// Update GetQueue chain
                this.children.GetQueueChain.Remove(data);
                this.children.GetQueueChain.Enqueue(data);
            }
        }

        return data;
    }

    public LpData2? TryGetChild(Identifier id)
    {
        LpData2? data;
        using (this.semaphore.Lock())
        {
            this.children = this.PrepareChildren();
            if (this.children.IdChain.TryGetValue(id, out data))
            {// Update GetQueue chain
                this.children.GetQueueChain.Remove(data);
                this.children.GetQueueChain.Enqueue(data);
            }

            return data;
        }
    }

    public bool DeleteChild(Identifier id)
    {
        using (this.semaphore.Lock())
        {
            this.children = this.PrepareChildren();
            if (this.children.IdChain.TryGetValue(id, out var data))
            {
                data.DeleteActual();
                return true;
            }
        }

        return false;
    }

    #endregion

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
        this.children = null;
        this.Goshujin = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GoshujinClass PrepareChildren()
    {
        if (this.children != null)
        {// Existing
            return this.children;
        }
        else if (CrystalHelper.IsValidFile(this.childrenFile))
        {// Load
            // this.Crystal.Storage.Save(ref this.childrenFile, memoryToBeShared, id);
            var result = this.Crystal.Storage.Load(this.childrenFile).Result;
        }
        else
        {// New
            return new GoshujinClass();
        }
    }
}
