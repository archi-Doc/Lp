// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

using System.Runtime.CompilerServices;
using ValueLink;

namespace LP.Crystal;

/*[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
public partial class LpData : BaseData
{
    public enum LpDataId
    {
        Deleted = -1,
        Credit = 0,
    }

    public LpData(ICrystalInternal crystal, BaseData? parent, Identifier identifier)
        : base(crystal, parent)
    {
        this.identifier = identifier;
    }

    [Link(Primary = true, Name = "GetQueue", Type = ChainType.QueueList)]
    public LpData()
    {
    }

    public new LpDataId DataId
    {
        get => (LpDataId)base.DataId;
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
    private bool childrenSaved = true;

    public int Count(LpDataId id)
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
        LpData? data;
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

    public LpData GetOrCreateChild(Identifier id)
    {
        LpData? data;
        using (this.semaphore.Lock())
        {
            this.children = this.PrepareChildren();
            if (!this.children.IdChain.TryGetValue(id, out data))
            {
                data = new LpData(this.Crystal, this, id);
                this.children.Add(data);
                this.childrenSaved = false;
            }
            else
            {// Update GetQueue chain
                this.children.GetQueueChain.Remove(data);
                this.children.GetQueueChain.Enqueue(data);
            }
        }

        return data;
    }

    public LpData? TryGetChild(Identifier id)
    {
        LpData? data;
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
                this.childrenSaved = false;
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

    protected override void SaveInternal(bool unload)
    {
        if (this.children != null)
        {
            foreach (var x in this.children)
            {
                x.SaveInternal(unload);
            }

            if (!this.childrenSaved)
            {
                try
                {
                    var b = TinyhandSerializer.SerializeObject(this.children);
                    this.Crystal.Storage.Save(ref this.childrenFile, new ByteArrayPool.ReadOnlyMemoryOwner(b), 0);
                    this.childrenSaved = true;
                }
                catch
                {
                }
            }
        }
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
            var result = this.Crystal.Storage.Load(this.childrenFile).Result;
            if (result.IsSuccess)
            {
                GoshujinClass? goshujin = null;
                try
                {
                    goshujin = TinyhandSerializer.DeserializeObject<GoshujinClass>(result.Data.Memory.Span);
                    if (goshujin is not null)
                    {
                        foreach (var x in goshujin)
                        {
                            x.Initialize(this.Crystal, this, true);
                        }
                    }
                }
                catch
                {
                }

                return goshujin ?? new GoshujinClass();
            }
            else
            {
                this.Crystal.Storage.Delete(this.childrenFile);
                return new GoshujinClass();
            }
        }
        else
        {// New
            return new GoshujinClass();
        }
    }
}*/
