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
[TinyhandObject(ExplicitKeyOnly = true, LockObject = "semaphore", ReservedKeys = 3)]
public partial class BaseData : IDataInternal
{
    protected BaseData()
    {
    }

    protected BaseData(ICrystalInternal crystal, BaseData? parent)
    {
        this.Crystal = crystal;
        this.Parent = parent;
    }

    public ICrystalInternal Crystal { get; private set; } = default!;

    public BaseData? Parent { get; private set; }

    public bool IsDeleted => this.DataId == -1;

    [Key(0)]
    public int DataId { get; set; } // -1: Deleted

    [Key(1)]
    protected DatumObject[] datumObject = Array.Empty<DatumObject>();

#pragma warning disable SA1214 // Readonly fields should appear before non-readonly fields
    protected readonly SemaphoreLock semaphore = new();
#pragma warning restore SA1214 // Readonly fields should appear before non-readonly fields

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

    #region IDataInternal

    ICrystalInternal IDataInternal.CrystalInternal => this.Crystal;

    DatumConstructor IDataInternal.Data => this.Crystal.Constructor;

    CrystalOptions IDataInternal.Options => this.Crystal.Options;

    void IDataInternal.DataToStorage<TData>(ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
    {// using (this.semaphore.Lock())
        var id = TData.StaticId;
        for (var i = 0; i < this.datumObject.Length; i++)
        {
            if (this.datumObject[i].DatumId == id)
            {
                this.Crystal.Storage.Save(ref this.datumObject[i].StorageId, ref this.datumObject[i].FileId, memoryToBeShared, id);
                return;
            }
        }
    }

    async Task<CrystalMemoryOwnerResult> IDataInternal.StorageToDatum<TDatum>()
    {// using (this.semaphore.Lock())
        var datumObject = this.TryGetDatumObject<TDatum>();
        if (!datumObject.IsValid)
        {
            return new(CrystalResult.NoData);
        }

        if (!datumObject.IsValidStorage)
        {
            return new(CrystalResult.NoData);
        }

        return await this.Crystal.Storage.Load(datumObject.StorageId, datumObject.FileId).ConfigureAwait(false);
    }

    void IDataInternal.DeleteStorage<TDatum>()
    {// using (this.semaphore.Lock())
        var datumObject = this.TryGetDatumObject<TDatum>();
        if (datumObject.IsValid)
        {
            this.Crystal.Storage.Delete(datumObject.StorageId, datumObject.FileId);
            return;
        }
    }

    /// <summary>
    /// Called from outside Flake, unloads DataObjects with matching id.
    /// </summary>
    /// <param name="id">The specified id.</param>
    /// <param name="unload"><see langword="true"/>; unload data.</param>
    void IDataInternal.SaveDatum(ushort id, bool unload)
    {
        using (this.semaphore.Lock())
        {
            for (var i = 0; i < this.datumObject.Length; i++)
            {
                if (this.datumObject[i].DatumId == id)
                {
                    this.datumObject[i].Datum?.Save();
                    if (unload)
                    {
                        this.datumObject[i].Datum?.Unload();
                        this.datumObject[i].Datum = null;
                    }

                    return;
                }
            }
        }
    }

    #endregion

    #region Main

    public LockOperation<TDatum> Lock<TDatum>()
       where TDatum : IDatum
    {
        var operation = new LockOperation<TDatum>(this);

        operation.Enter();
        if (this.IsDeleted)
        {// Removed
            operation.SetResult(CrystalResult.Deleted);
            operation.Exit();
            return operation;
        }

        var dataObject = this.GetOrCreateDatumObject<TDatum>();
        if (dataObject.Datum is not TDatum data)
        {// No data
            operation.SetResult(CrystalResult.DatumNotRegistered);
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

            this.SaveInternal(unload);

            for (var i = 0; i < this.datumObject.Length; i++)
            {
                this.datumObject[i].Datum?.Save();
                if (unload)
                {
                    this.datumObject[i].Datum?.Unload();
                    this.datumObject[i].Datum = null;
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
            this.DeleteActual();
        }
        else
        {
            using (this.Parent.semaphore.Lock())
            {
                this.DeleteActual();
            }
        }

        return true;
    }

    protected void DeleteActual()
    {// using (this.Parent.semaphore.Lock())
        using (this.semaphore.Lock())
        {
            var array = this.ChildrenInternal.ToArray();
            foreach (var x in array)
            {
                x.DeleteActual();
            }

            this.DeleteInternal();

            for (var i = 0; i < this.datumObject.Length; i++)
            {
                this.Crystal.Storage.Delete(this.datumObject[i].StorageId, this.datumObject[i].FileId);
                this.datumObject[i].Datum?.Unload();
                this.datumObject[i].Datum = null;
                this.datumObject[i].FileId = 0;
            }

            this.datumObject = Array.Empty<DatumObject>();
            this.Parent = null;
            this.DataId = -1;
        }
    }

    #endregion

    #region Abstract

    protected virtual IEnumerator<BaseData> EnumerateInternal()
    {
        yield break;
    }

    protected virtual void DeleteInternal()
    {
    }

    protected virtual void SaveInternal(bool unload)
    {
    }

    /*protected internal virtual void UnloadInternal()
    {
    }*/

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

    private DatumObject GetOrCreateDatumObject<TDatum>()
        where TDatum : IDatum
    {// using (this.semaphore.Lock())
        var id = TDatum.StaticId;
        for (var i = 0; i < this.datumObject.Length; i++)
        {
            if (this.datumObject[i].DatumId == id)
            {
                if (this.datumObject[i].Datum == null)
                {
                    if (this.Crystal.Constructor.TryGetConstructor(id) is { } ctr1)
                    {
                        this.datumObject[i].Datum = ctr1(this);
                    }
                }

                return this.datumObject[i];
            }
        }

        var newObject = default(DatumObject);
        newObject.DatumId = id;
        if (this.Crystal.Constructor.TryGetConstructor(id) is { } ctr2)
        {
            newObject.Datum = ctr2(this);
        }

        if (newObject.Datum == null)
        {
            return default;
        }

        var n = this.datumObject.Length;
        Array.Resize(ref this.datumObject, n + 1);
        this.datumObject[n] = newObject;
        return newObject;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DatumObject TryGetDatumObject<TDatum>()
        where TDatum : IDatum
    {// using (this.semaphore.Lock())
        var id = TDatum.StaticId;
        for (var i = 0; i < this.datumObject.Length; i++)
        {
            if (this.datumObject[i].DatumId == id)
            {
                return this.datumObject[i];
            }
        }

        return default;
    }
}
