// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

using CrystalData.Datum;
using ValueLink;

namespace LP.Crystal;

[TinyhandObject(ExplicitKeyOnly = true, Journal = true)]
[ValueLinkObject]
public partial record LpData : BaseData
{
    public enum LpDataId
    {
        Deleted = -1,
        Credit = 0,
    }

    public LpData(IBigCrystal crystal, BaseData? parent, Identifier identifier)
        : base(crystal, parent)
    {
        this.identifier = identifier;
    }

    [Link(Name = "GetQueue", Type = ChainType.QueueList)]
    public LpData()
    {
    }

    #region FieldAndProperty

    [IgnoreMember]
    public LpDataId DataId
    {
        get => (LpDataId)this.BaseDataId;
        set => this.BaseDataId = (int)value;
    }

    public Identifier Identifier => this.identifier;

    [Key(4, AddProperty = "Id")]
    [Link(Primary = true, Unique = true, Name = "Id", AddValue = false, Type = ChainType.Unordered)]
    [Link(Name = "OrderedId", AddValue = false, Type = ChainType.Ordered)]
    private Identifier identifier = default!;

    [Key(5)]
    private GoshujinClass? children;

    #endregion

    /*void ITinyhandCustomJournal.WriteCustomRecord(ref TinyhandWriter writer)
    {
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        return false;
    }*/

    /*public int Count(LpDataId id)
    {
        var intId = (int)id;
        var count = 0;
        using (this.semaphore.Lock())
        {
            foreach (var x in this.GetChildren())
            {
                if (x.DataId == intId)
                {
                    count++;
                }
            }
        }

        return count;
    }*/

    #region Child

    public LockOperation<TDatum> LockChild<TDatum>(Identifier id)
        where TDatum : IDatum
    {
        LpData? data;
        using (this.semaphore.Lock())
        {
            if (this.children == null)
            {
                return default;
            }

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

        return data.Lock<TDatum>();
    }

    public LpData GetOrCreateChild(Identifier id)
    {
        LpData? data;
        using (this.semaphore.Lock())
        {
            this.children ??= new();
            if (!this.children.IdChain.TryGetValue(id, out data))
            {
                data = new LpData(this.BigCrystal, this, id);
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

    public LpData? TryGetChild(Identifier id)
    {
        LpData? data;
        using (this.semaphore.Lock())
        {
            if (this.children == null)
            {
                return null;
            }

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
            if (this.children == null)
            {
                return false;
            }

            if (this.children.IdChain.TryGetValue(id, out var data))
            {
                data.DeleteActual();
                return true;
            }
        }

        return false;
    }

    #endregion

    public override LpData[] GetChildren()
    {
        if (this.children == null)
        {
            return Array.Empty<LpData>();
        }

        return Array.Empty<LpData>();
        // return this.children.GetArray();
    }

    protected override void DeleteInternal()
    {
        this.children = null;
        this.Goshujin = null;
    }
}
