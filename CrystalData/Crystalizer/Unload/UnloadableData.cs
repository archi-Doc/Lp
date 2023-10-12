// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204

namespace CrystalData;

[TinyhandObject(Journal = true)]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class DesignSerializable
{
    public DesignSerializable()
    {
    }

    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public DesignSerializable Class { get; set; } = new();

    [Key(2)]
    public UnloadableData<DesignSerializable> UnloadableClass { get; set; } = new();
}

[TinyhandObject(Journal = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record CrystalClass
{// This is it. This class is the crystal of state-of-the-art data management technology.
    public CrystalClass()
    {
    }

    [Key(0)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    [Key(1)]
    [Link(Type = ChainType.Ordered)]
    public string Name { get; set; } = string.Empty;

    [Key(2)]
    public UnloadableData<CrystalClass> Child { get; set; } = new();

    [Key(3)]
    public UnloadableData<CrystalClass.GoshujinClass> Children { get; set; } = new();

    [Key(4)]
    public UnloadableData<byte[]> ByteArray { get; set; } = new();
}

/// <summary>
/// <see cref="UnloadableData{TData}"/> is a subset of <see cref="CrystalObject{TData}"/>, allowing for the persistence of partial data.
/// </summary>
/// <typeparam name="TData">The type of data.</typeparam>
[TinyhandObject(ExplicitKeyOnly = true)]
public sealed partial class UnloadableData<TData> : SemaphoreLock, ITreeObject
// where TData : ITinyhandSerialize<TData>
{
    public const int MaxHistories = 4;

    #region FieldAndProperty

    [Key(0)]
    private StorageConfiguration? storageConfiguration; // using (this.Lock())

    [Key(1)]
    private TData? data; // using (this.Lock())

    [Key(2)]
    private StorageId storageId0;

    [Key(3)]
    private StorageId storageId1;

    [Key(4)]
    private StorageId storageId2;

    [Key(5)]
    private StorageId storageId3;

    [IgnoreMember]
    ITreeRoot? ITreeObject.TreeRoot { get; set; }

    [IgnoreMember]
    ITreeObject? ITreeObject.TreeParent { get; set; }

    [IgnoreMember]
    int ITreeObject.TreeKey { get; set; }

    #endregion

    public UnloadableData()
    {
    }

    public async ValueTask<TData> Get()
    {
        if (this.data is { } data)
        {
            return data;
        }

        await this.EnterAsync().ConfigureAwait(false); // using (this.Lock())
        try
        {
            if (this.data is null)
            {// PrepareAndLoad
                await this.PrepareAndLoadInternal().ConfigureAwait(false);
            }

            if (this.data is null)
            {// Reconstruct
                this.data = TinyhandSerializer.Reconstruct<TData>();
            }

            return this.data;
        }
        finally
        {
            this.Exit();
        }
    }

    public void Set(TData data)
    {
        using (this.Lock())
        {
            this.data = data;
        }
    }

    public void SetStorageConfiguration(StorageConfiguration storageConfiguration)
    {
        using (this.Lock())
        {
            this.storageConfiguration = storageConfiguration;
        }
    }

    public async Task<bool> Save(UnloadMode unloadMode)
    {
        await this.EnterAsync().ConfigureAwait(false); // using (this.Lock())
        try
        {
            if (this.data is null)
            {// No data
                return true;
            }

            if (((ITreeObject)this).TreeRoot is not ICrystal crystal)
            {// No root
                return true;
            }

            // Save children
            var treeObject = this.data as ITreeObject;
            if (treeObject is not null)
            {
                var result = await treeObject.Save(unloadMode).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
            }

            var journalPosition = crystal.AddJournal();

            // Serialize and get hash.
            var options = unloadMode.IsUnload() ? TinyhandSerializerOptions.Unload : TinyhandSerializerOptions.Standard;
            SerializeHelper.Serialize<TData>(this.data, options, out var owner);
            var byteArray = TinyhandSerializer.Serialize(this.data, options);
            var hash = FarmHash.Hash64(byteArray);

            if (!this.storageId0.HashEquals(hash))
            {// Different data
                // Put
                var storage = crystal.GetStorage(this.storageConfiguration);
                var storageId = new StorageId(journalPosition, hash, 0);
                storage.PutAndForget(ref storageId, owner.AsReadOnly());

                // Update histories
                var numberOfHistories = 3;
                if (numberOfHistories <= 1)
                {
                    this.storageId0 = storageId;
                }
                else if (numberOfHistories == 2)
                {
                    if (this.storageId1.IsValid)
                    {
                        storage.DeleteAndForget(ref this.storageId1);
                    }

                    this.storageId1 = this.storageId0;
                    this.storageId0 = storageId;
                }
                else if (numberOfHistories == 3)
                {
                    if (this.storageId2.IsValid)
                    {
                        storage.DeleteAndForget(ref this.storageId2);
                    }

                    this.storageId2 = this.storageId1;
                    this.storageId1 = this.storageId0;
                    this.storageId0 = storageId;
                }
                else if (numberOfHistories >= MaxHistories)
                {
                    if (this.storageId3.IsValid)
                    {
                        storage.DeleteAndForget(ref this.storageId3);
                    }

                    this.storageId3 = this.storageId2;
                    this.storageId2 = this.storageId1;
                    this.storageId1 = this.storageId0;
                    this.storageId0 = storageId;
                }
            }

            owner.Return();

            if (unloadMode.IsUnload())
            {// Unload
                this.data = default;
            }
        }
        finally
        {
            this.Exit();
        }

        return true;
    }

    public void Delete()
    {
        ITreeObject? treeObject;
        StorageConfiguration? configuration;
        using (this.Lock())
        {
            treeObject = this.data as ITreeObject;
            configuration = this.storageConfiguration;

            this.storagePoint = default;
            this.data = default;
        }

        if (((ITreeObject)this).TreeRoot is ICrystal crystal)
        {// Delete storage
            var storage = crystal.GetStorage(configuration);
            var storageId = 123ul;
            storage.DeleteAndForget(ref storageId);
        }

        if (treeObject is not null)
        {
            treeObject.Delete();
        }
    }

    private async Task PrepareAndLoadInternal()
    {// using (this.Lock())
        if (this.data is not null)
        {
            return;
        }

        Crystalizer crystalizer = default!;
        var storage = crystalizer.ResolveStorage(this.storageConfiguration!);
        var storageId = 123ul;
        var result = await storage.GetAsync(ref storageId).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return;
        }

        // Deserialize
        try
        {
            this.data = TinyhandSerializer.Deserialize<TData>(result.Data.Span);
        }
        catch
        {
        }
        finally
        {
            result.Return();
        }
    }
}
