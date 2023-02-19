// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Runtime.CompilerServices;

namespace CrystalData;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1401

/// <summary>
/// <see cref="BaseData"/> is an independent class that holds data at a single point in the hierarchical structure.
/// </summary>
[TinyhandObject(ExplicitKeyOnly = true, LockObject = "semaphore", ReservedKeys = 2)]
public abstract partial class BaseData : IDataInternal
{
    internal BaseData()
    {
    }

    internal BaseData(ICrystalInternal zen)
    {
        this.Crystal = zen;
    }

    public ICrystalInternal Crystal { get; private set; } = default!;

    public BaseData? Parent { get; private set; }

    public bool IsDeleted => this.DataId == -1;

    [Key(0)]
    public int DataId { get; private set; } // -1: Removed

    [Key(1)]
    private protected DatumObject[] datumObject = Array.Empty<DatumObject>();

    protected readonly SemaphoreLock semaphore = new();

    #region Enumerable

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

#endregion

    #region IFlakeInternal

    ICrystalInternal IDataInternal.ZenInternal => this.Crystal;

    DataConstructor IDataInternal.Data => this.Crystal.Constructor;

    CrystalOptions IDataInternal.Options => this.Crystal.Options;

    void IDataInternal.DataToStorage<TData>(ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
    {// using (this.semaphore.Lock())
        var id = TData.StaticId;
        for (var i = 0; i < this.datumObject.Length; i++)
        {
            if (this.datumObject[i].Id == id)
            {
                this.Crystal.Storage.Save(ref this.datumObject[i].File, memoryToBeShared, id);
                return;
            }
        }
    }

    async Task<CrystalMemoryOwnerResult> IDataInternal.StorageToData<TData>()
    {// using (this.semaphore.Lock())
        var dataObject = this.TryGetDataObject<TData>();
        if (!dataObject.IsValid)
        {
            return new(CrystalResult.NoData);
        }

        var file = dataObject.File;
        if (!CrystalHelper.IsValidFile(file))
        {
            return new(CrystalResult.NoData);
        }

        return await this.Crystal.Storage.Load(file);
    }

    void IDataInternal.DeleteStorage<TData>()
    {// using (this.semaphore.Lock())
        var dataObject = this.TryGetDataObject<TData>();
        if (dataObject.IsValid)
        {
            this.Crystal.Storage.Delete(dataObject.File);
            return;
        }
    }

    /// <summary>
    /// Called from outside Flake, unloads DataObjects with matching id.
    /// </summary>
    /// <param name="id">The specified id.</param>
    /// <param name="unload"><see langword="true"/>; unload data.</param>
    void IDataInternal.SaveData(int id, bool unload)
    {
        using (this.semaphore.Lock())
        {
            for (var i = 0; i < this.datumObject.Length; i++)
            {
                if (this.datumObject[i].Id == id)
                {
                    this.datumObject[i].Data?.Save();
                    if (unload)
                    {
                        this.datumObject[i].Data?.Unload();
                        this.datumObject[i].Data = null;
                    }

                    return;
                }
            }
        }
    }

    #endregion

    #region Main

    public LockOperation<TData> Lock<TData>()
       where TData : IDatum
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

            for (var i = 0; i < this.datumObject.Length; i++)
            {
                this.datumObject[i].Data?.Save();
                if (unload)
                {
                    this.datumObject[i].Data?.Unload();
                    this.datumObject[i].Data = null;
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
        if (this.Parent == null)
        {
            this.DeleteData();
        }
        else
        {
            using (this.Parent.semaphore.Lock())
            {
                this.DeleteData();
            }
        }

        return true;
    }

    protected void DeleteData()
    {// using (this.Parent.semaphore.Lock())
        using (this.semaphore.Lock())
        {
            var array = this.ChildrenInternal.ToArray();
            foreach (var x in array)
            {
                x.DeleteData();
            }

            this.DeleteInternal();

            for (var i = 0; i < this.datumObject.Length; i++)
            {
                this.Crystal.Storage.Delete(this.datumObject[i].File);
                this.datumObject[i].Data?.Unload();
                this.datumObject[i].Data = null;
                this.datumObject[i].File = 0;
            }

            this.datumObject = Array.Empty<DatumObject>();
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

    protected internal void Initialize(ICrystalInternal crystal, BaseData? parent, bool initializeChildren)
    {
        this.Crystal = crystal;
        this.Parent = parent;

        if (initializeChildren)
        {
            foreach (var x in this.ChildrenInternal)
            {
                x.Initialize(crystal, this, initializeChildren);
            }
        }
    }

    private DatumObject GetOrCreateDataObject<TData>()
        where TData : IDatum
    {// using (this.semaphore.Lock())
        var id = TData.StaticId;
        for (var i = 0; i < this.datumObject.Length; i++)
        {
            if (this.datumObject[i].Id == id)
            {
                if (this.datumObject[i].Data == null)
                {
                    if (this.Crystal.Constructor.TryGetConstructor(id) is { } ctr1)
                    {
                        this.datumObject[i].Data = ctr1(this);
                    }
                }

                return this.datumObject[i];
            }
        }

        var newObject = default(DatumObject);
        newObject.Id = id;
        if (this.Crystal.Constructor.TryGetConstructor(id) is { } ctr2)
        {
            newObject.Data = ctr2(this);
        }

        if (newObject.Data == null)
        {
            return default;
        }

        var n = this.datumObject.Length;
        Array.Resize(ref this.datumObject, n + 1);
        this.datumObject[n] = newObject;
        return newObject;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DatumObject TryGetDataObject<TData>()
        where TData : IDatum
    {// using (this.semaphore.Lock())
        var id = TData.StaticId;
        for (var i = 0; i < this.datumObject.Length; i++)
        {
            if (this.datumObject[i].Id == id)
            {
                return this.datumObject[i];
            }
        }

        return default;
    }
}
