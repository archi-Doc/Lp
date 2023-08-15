// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1124 // Do not use regions

using CrystalData.Datum;

namespace CrystalData;

[TinyhandObject(ExplicitKeyOnly = true, Journal = true)]
[ValueLinkObject]
public partial record RecursiveData<TKey> : BaseData
{
    public RecursiveData(IBigCrystal crystal, BaseData? parent, TKey key)
        : base(crystal, parent)
    {
        this.key = key;
    }

    public RecursiveData()
    {
    }

    #region FieldAndProperty

    [Key(4, AddProperty = "Key")]
    [Link(Primary = true, Unique = true, Name = "Key", AddValue = false, Type = ChainType.Unordered)]
    [Link(Name = "OrderedKey", AddValue = false, Type = ChainType.Ordered)]
    private TKey key = default!;

    [Key(5)]
    private GoshujinClass? children;

    #endregion

    #region Child

    public LockOperation<TDatum> LockChild<TDatum>(TKey key)
        where TDatum : IDatum
    {
        RecursiveData<TKey>? data;
        using (this.semaphore.Lock())
        {
            if (this.children == null)
            {
                return default;
            }

            if (!this.children.KeyChain.TryGetValue(key, out data))
            {
                return default;
            }
        }

        return data.Lock<TDatum>();
    }

    public RecursiveData<TKey> GetOrCreateChild(TKey key)
    {
        RecursiveData<TKey>? data;
        using (this.semaphore.Lock())
        {
            this.children ??= new();
            if (!this.children.KeyChain.TryGetValue(key, out data))
            {
                data = new(this.BigCrystal, this, key);
                this.children.Add(data);
            }
        }

        return data;
    }

    public RecursiveData<TKey>? TryGetChild(TKey key)
    {
        RecursiveData<TKey>? data;
        using (this.semaphore.Lock())
        {
            if (this.children == null)
            {
                return null;
            }

            if (!this.children.KeyChain.TryGetValue(key, out data))
            {
                return default;
            }

            return data;
        }
    }

    public bool DeleteChild(TKey key)
    {
        using (this.semaphore.Lock())
        {
            if (this.children == null)
            {
                return false;
            }

            if (this.children.KeyChain.TryGetValue(key, out var data))
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
}
