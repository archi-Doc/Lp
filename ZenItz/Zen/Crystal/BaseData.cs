// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Runtime.CompilerServices;
using ZenItz;

namespace CrystalData;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1401

/// <summary>
/// <see cref="BaseData"/> is an independent class that holds data at a single point in the hierarchical structure.
/// </summary>
[TinyhandObject(ExplicitKeyOnly = true, LockObject = "semaphore", ReservedKeys = 2)]
public abstract partial class BaseData : IFlakeInternal
{
    internal BaseData()
    {
    }

    internal BaseData(IZenInternal zen)
    {
        this.Zen = zen;
    }

    public IZenInternal Zen { get; private set; } = default!;

    public BaseData? Parent { get; private set; }

    public bool IsDeleted => this.DataId == -1;

    [Key(0)]
    public int DataId { get; private set; } // -1: Removed

    [Key(1)]
    private protected DataObject[] dataObject = Array.Empty<DataObject>();

    protected readonly SemaphoreLock semaphore = new();

    private readonly struct Enumerator : IEnumerable<BaseData>
    {
        public Enumerator(BaseData baseData)
        {
            this.baseData = baseData;
        }

        public IEnumerator<BaseData> GetEnumerator()
            => this.baseData.EnumerateInternal();

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

        private readonly BaseData baseData;
    }

    protected IEnumerable<BaseData> ChildrenInternal => new Enumerator(this);

    #region IFlakeInternal

    IZenInternal IFlakeInternal.ZenInternal => this.Zen;

    DataConstructor IFlakeInternal.Data => this.Zen.Constructor;

    ZenOptions IFlakeInternal.Options => this.Zen.Options;

    void IFlakeInternal.DataToStorage<TData>(ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
    {// using (this.semaphore.Lock())
        var id = TData.StaticId;
        for (var i = 0; i < this.dataObject.Length; i++)
        {
            if (this.dataObject[i].Id == id)
            {
                this.Zen.Storage.Save(ref this.dataObject[i].File, memoryToBeShared, id);
                return;
            }
        }
    }

    async Task<ZenMemoryOwnerResult> IFlakeInternal.StorageToData<TData>()
    {// using (this.semaphore.Lock())
        var dataObject = this.TryGetDataObject<TData>();
        if (!dataObject.IsValid)
        {
            return new(ZenResult.NoData);
        }

        var file = dataObject.File;
        if (!ZenHelper.IsValidFile(file))
        {
            return new(ZenResult.NoData);
        }

        return await this.Zen.Storage.Load(file);
    }

    void IFlakeInternal.DeleteStorage<TData>()
    {// using (this.semaphore.Lock())
        var dataObject = this.TryGetDataObject<TData>();
        if (dataObject.IsValid)
        {
            this.Zen.Storage.Delete(dataObject.File);
            return;
        }
    }

    /// <summary>
    /// Called from outside Flake, unloads DataObjects with matching id.
    /// </summary>
    /// <param name="id">The specified id.</param>
    /// <param name="unload"><see langword="true"/>; unload data.</param>
    void IFlakeInternal.SaveData(int id, bool unload)
    {
        using (this.semaphore.Lock())
        {
            for (var i = 0; i < this.dataObject.Length; i++)
            {
                if (this.dataObject[i].Id == id)
                {
                    this.dataObject[i].Data?.Save();
                    if (unload)
                    {
                        this.dataObject[i].Data?.Unload();
                        this.dataObject[i].Data = null;
                    }

                    return;
                }
            }
        }
    }

    #endregion

    #region Main

    public LockOperation<TData> Lock<TData>()
       where TData : IData
    {
        var operation = new LockOperation<TData>(this);

        operation.Enter();
        if (this.IsDeleted)
        {// Removed
            operation.Exit();
            return operation;
        }

        var dataObject = this.GetOrCreateDataObject<TData>();
        if (dataObject.Data is not TData data)
        {// No data
            operation.Exit();
            return operation;
        }

        operation.SetData(data);
        return operation;
    }

    public void Save(bool unload = false)
    {
        using (this.semaphore.Lock())
        {
            if (this.IsDeleted)
            {
                return;
            }

            foreach (var x in this.ChildrenInternal)
            {
                x.Save(unload);
            }

            for (var i = 0; i < this.dataObject.Length; i++)
            {
                this.dataObject[i].Data?.Save();
                if (unload)
                {
                    this.dataObject[i].Data?.Unload();
                    this.dataObject[i].Data = null;
                }
            }
        }
    }

    /// <summary>
    /// Delete this <see cref="BaseData"/> from the parent and delete the data.
    /// </summary>
    /// <returns><see langword="true"/>; this <see cref="BaseData"/> is successfully deleted.</returns>
    public bool Delete()
    {
        /*if (this.Parent == null)
        {// The root flake cannot be removed directly.
            return false;
        }*/

        using (this.Parents.semaphore.Lock())
        {
            this.DeleteData();
        }

        return true;
    }

    protected void DeleteData()
    {
        using (this.semaphore.Lock())
        {
            var array = this.ChildrenInternal.ToArray();
            foreach (var x in array)
            {
                x.DeleteData();
            }

            this.DeleteInternal();

            for (var i = 0; i < this.dataObject.Length; i++)
            {
                this.Zen.Storage.Delete(this.dataObject[i].File);
                this.dataObject[i].Data?.Unload();
                this.dataObject[i].Data = null;
                this.dataObject[i].File = 0;
            }

            this.dataObject = Array.Empty<DataObject>();
            this.Parent = null;
            this.DataId = -1;
        }
    }

    #endregion

    #region Abstract

    protected abstract IEnumerator<BaseData> EnumerateInternal();

    // protected abstract void SaveInternal(bool unload);

    protected abstract void DeleteInternal();

    #endregion

    internal void DeserializePostProcess<TData>(Crystal<TData> crystal, BaseData? parent = null)
        where TData : CrystalData.BaseData
    {
        this.Zen = crystal;
        this.Parent = parent;

        foreach (var x in this.ChildrenInternal)
        {
            x.DeserializePostProcess(crystal, this);
        }
    }

    private DataObject GetOrCreateDataObject<TData>()
        where TData : IData
    {// using (this.semaphore.Lock())
        var id = TData.StaticId;
        for (var i = 0; i < this.dataObject.Length; i++)
        {
            if (this.dataObject[i].Id == id)
            {
                if (this.dataObject[i].Data == null)
                {
                    if (this.Zen.Constructor.TryGetConstructor(id) is { } ctr1)
                    {
                        this.dataObject[i].Data = ctr1(this);
                    }
                }

                return this.dataObject[i];
            }
        }

        var newObject = default(DataObject);
        newObject.Id = id;
        if (this.Zen.Constructor.TryGetConstructor(id) is { } ctr2)
        {
            newObject.Data = ctr2(this);
        }

        if (newObject.Data == null)
        {
            return default;
        }

        var n = this.dataObject.Length;
        Array.Resize(ref this.dataObject, n + 1);
        this.dataObject[n] = newObject;
        return newObject;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DataObject TryGetDataObject<TData>()
        where TData : IData
    {// using (this.semaphore.Lock())
        var id = TData.StaticId;
        for (var i = 0; i < this.dataObject.Length; i++)
        {
            if (this.dataObject[i].Id == id)
            {
                return this.dataObject[i];
            }
        }

        return default;
    }
}
