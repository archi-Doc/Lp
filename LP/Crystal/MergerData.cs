// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

using System.Runtime.CompilerServices;
using Arc.Collections;
using CrystalData.Datum;
using ValueLink;

namespace LP.Crystal;

[TinyhandObject(ExplicitKeyOnly = true)]
[ValueLinkObject]
public partial class MergerData : BaseData
{
    public MergerData(IBigCrystal crystal, BaseData? parent, Identifier identifier)
        : base(crystal, parent)
    {
        this.identifier = identifier;
    }

    // [Link(Primary = true, Name = "GetQueue", Type = ChainType.QueueList)]
    public MergerData()
    {
    }

    public new LpData.LpDataId DataId
    {
        get => (LpData.LpDataId)base.DataId;
        set => base.DataId = (int)value;
    }

    public Identifier Identifier => this.identifier;

    [Key(4)]
    [Link(Primary = true, Name = "Id", NoValue = true, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", Type = ChainType.Ordered)]
    private Identifier identifier = default!;

    [Key(5)]
    private ushort childrenStorage;

    [Key(6)]
    private ulong childrenFile;

    private GoshujinClass? children;
    private bool childrenSaved = true;
    private UnorderedLinkedList<BaseData>.Node? node;

    public int Count(LpData.LpDataId id)
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

    public bool IsInMemory => this.node != null;

    #region Child

    public LockOperation<TDatum> LockChild<TDatum>(Identifier id)
        where TDatum : IDatum
    {
        MergerData? data;
        using (this.semaphore.Lock())
        {
            this.children = this.PrepareChildren();
            if (this.children.IdChain.TryGetValue(id, out data))
            {// Update GetQueue chain
                // this.children.GetQueueChain.Remove(data);
                // this.children.GetQueueChain.Enqueue(data);
            }
            else
            {
                return default;
            }
        }

        return data.Lock<TDatum>();
    }

    public MergerData GetOrCreateChild(Identifier id)
    {
        MergerData? data;
        using (this.semaphore.Lock())
        {
            this.children = this.PrepareChildren();
            if (!this.children.IdChain.TryGetValue(id, out data))
            {
                data = new MergerData(this.BigCrystal, this, id);
                this.children.Add(data);
                this.childrenSaved = false;
            }
            else
            {// Update GetQueue chain
                // this.children.GetQueueChain.Remove(data);
                // this.children.GetQueueChain.Enqueue(data);
            }
        }

        return data;
    }

    public MergerData? TryGetChild(Identifier id)
    {
        MergerData? data;
        using (this.semaphore.Lock())
        {
            this.children = this.PrepareChildren();
            if (this.children.IdChain.TryGetValue(id, out data))
            {// Update GetQueue chain
                // this.children.GetQueueChain.Remove(data);
                // this.children.GetQueueChain.Enqueue(data);
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
            this.TryLoadChildren();
            if (this.children == null)
            {
                yield break;
            }
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

        if (this.node != null)
        {
            this.BigCrystal.Himo.RemoveParentData(this.node);
            this.node = null;
        }
    }

    protected override void SaveInternal(bool unload)
    {
        if (this.children != null)
        {
            /*foreach (var x in this.children)
            {
                x.SaveInternal(unload);
            }*/

            if (!this.childrenSaved)
            {
                try
                {
                    var b = TinyhandSerializer.SerializeObject(this.children);
                    this.BigCrystal.StorageGroup.PutAndForget(ref this.childrenStorage, ref this.childrenFile, new ByteArrayPool.ReadOnlyMemoryOwner(b), 0);
                    this.childrenSaved = true;
                }
                catch
                {
                }
            }

            if (unload)
            {
                this.children.Clear();
                this.children = null;
            }
        }

        if (this.node != null && unload)
        {
            this.BigCrystal.Himo.RemoveParentData(this.node);
            this.node = null;
        }
    }

    /*protected override void UnloadInternal()
    {
    }*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GoshujinClass PrepareChildren()
    {
        if (this.node == null && this.Parent != null)
        {
            this.node = this.BigCrystal.Himo.AddParentData(this);
        }

        if (this.children != null)
        {// Existing
            return this.children;
        }
        else if (this.childrenStorage != 0)
        {// Load
            var result = this.BigCrystal.StorageGroup.GetAsync(this.childrenStorage, this.childrenFile).Result;
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
                            x.Initialize(this.BigCrystal, this, true);
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
                this.BigCrystal.StorageGroup.DeleteAndForget(ref this.childrenStorage, ref this.childrenFile);
                return new GoshujinClass();
            }
        }
        else
        {// New
            return new GoshujinClass();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryLoadChildren()
    {
        if (this.children == null && this.childrenStorage != 0 && this.childrenFile != 0)
        {// Load
            var result = this.BigCrystal.StorageGroup.GetAsync(this.childrenStorage, this.childrenFile).Result;
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
                            x.Initialize(this.BigCrystal, this, true);
                        }

                        this.children = goshujin;
                    }
                }
                catch
                {
                    this.BigCrystal.StorageGroup.DeleteAndForget(ref this.childrenStorage, ref this.childrenFile);
                }
            }
            else
            {
                this.BigCrystal.StorageGroup.DeleteAndForget(ref this.childrenStorage, ref this.childrenFile);
            }
        }
    }
}
