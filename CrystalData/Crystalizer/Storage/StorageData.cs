// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace CrystalData;

[TinyhandObject(Tree = true)]
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
    public StorageData<DesignSerializable> UnloadableClass { get; set; } = new();
}

[TinyhandObject(Tree = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record AdvancedClass
{// This is it. This class is the crystal of the most advanced data management architecture I've reached so far.
    public static void Register(IUnitCrystalContext context)
    {
        context.AddCrystal<AdvancedClass>(
            new()
            {
                SaveFormat = SaveFormat.Utf8,
                SavePolicy = SavePolicy.Periodic,
                SaveInterval = TimeSpan.FromMinutes(10),
                FileConfiguration = new GlobalFileConfiguration("CrystalClassMain.tinyhand"),
                BackupFileConfiguration = new GlobalFileConfiguration("CrystalClassBackup.tinyhand"),
                StorageConfiguration = GlobalStorageConfiguration.Default,
                /*StorageConfiguration = new SimpleStorageConfiguration(
                    new GlobalDirectoryConfiguration("MainStorage"),
                    new GlobalDirectoryConfiguration("BackupStorage")),*/
                NumberOfFileHistories = 2,
            });

        context.TrySetJournal(new SimpleJournalConfiguration(new S3DirectoryConfiguration("TestBucket", "Journal")));
    }

    public AdvancedClass()
    {
    }

    [Key(0, AddProperty = "Id", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    private int id;

    [Key(1, AddProperty = "Name")]
    [Link(Type = ChainType.Ordered)]
    private string name = string.Empty;

    [Key(2, AddProperty = "Child", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private StorageData<AdvancedClass> child = new();

    [Key(3, AddProperty = "Children", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private StorageData<AdvancedClass.GoshujinClass> children = new();

    [Key(4, AddProperty = "ByteArray", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private StorageData<byte[]> byteArray = new();
}

/// <summary>
/// <see cref="StorageData{TData}"/> is a subset of <see cref="CrystalObject{TData}"/>, allowing for the persistence of partial data.
/// </summary>
/// <typeparam name="TData">The type of data.</typeparam>
[TinyhandObject(ExplicitKeyOnly = true)]
public sealed partial class StorageData<TData> : SemaphoreLock, ITreeObject, IStorageData
// where TData : ITinyhandSerialize<TData>
{
    public const int MaxHistories = 3; // 4

    #region FieldAndProperty

    // [Key(0)]
    // private StorageConfiguration? storageConfiguration; // using (this.Lock())

    [IgnoreMember]
    private TData? data;

    [Key(0)]
    private StorageId storageId0;

    [Key(1)]
    private StorageId storageId1;

    [Key(2)]
    private StorageId storageId2;

    // [Key(3)]
    // private StorageId storageId3;

    [IgnoreMember]
    ITreeRoot? ITreeObject.TreeRoot { get; set; }

    [IgnoreMember]
    ITreeObject? ITreeObject.TreeParent { get; set; }

    [IgnoreMember]
    int ITreeObject.TreeKey { get; set; }

    #endregion

    public StorageData()
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
                // this.PrepareData() is called from PrepareAndLoadInternal().
            }

            if (this.data is null)
            {// Reconstruct
                this.data = TinyhandSerializer.Reconstruct<TData>();
                this.PrepareData(0);
            }

            return this.data;
        }
        finally
        {
            this.Exit();
        }
    }

    public void Set(TData data, int sizeHint = 0)
    {// Journaling is not supported.
        using (this.Lock())
        {
            this.data = data;
        }

        if (sizeHint > 0)
        {
        }
    }

    /*public void SetStorageConfiguration(StorageConfiguration storageConfiguration)
    {
        using (this.Lock())
        {
            this.storageConfiguration = storageConfiguration;
        }
    }*/

    public Type DataType
        => typeof(TData);

    public async Task<bool> Save(UnloadMode unloadMode)
    {
        if (this.data is null)
        {// No data
            return true;
        }

        await this.EnterAsync().ConfigureAwait(false); // using (this.Lock())
        try
        {
            if (this.data is null)
            {// No data
                return true;
            }

            if (((ITreeObject)this).TreeRoot is not ICrystal crystal)
            {// No crystal
                return true;
            }

            // Save children
            if (this.data is ITreeObject treeObject)
            {
                var result = await treeObject.Save(unloadMode).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
            }

            var currentPosition = crystal.Journal is null ? Waypoint.ValidJournalPosition : crystal.Journal.GetCurrentPosition();

            // Serialize and get hash.
            SerializeHelper.Serialize<TData>(this.data, TinyhandSerializerOptions.Standard, out var owner);
            var dataSize = owner.Span.Length;
            var hash = FarmHash.Hash64(owner.Span);

            if (hash != this.storageId0.Hash)
            {// Different data
                // Put
                ulong fileId = 0;
                crystal.Storage.PutAndForget(ref fileId, owner.AsReadOnly());
                var storageId = new StorageId(currentPosition, fileId, hash);

                // Update histories
                this.AddInternal(crystal, storageId);

                // Journal
                AddJournal();
                void AddJournal()
                {
                    if (((ITreeObject)this).TryGetJournalWriter(out var root, out var writer, true) == true)
                    {
                        if (this is ITinyhandCustomJournal tinyhandCustomJournal)
                        {
                            tinyhandCustomJournal.WriteCustomLocator(ref writer);
                        }

                        writer.Write(JournalRecord.AddStorage);
                        TinyhandSerializer.SerializeObject(ref writer, storageId);
                        root.AddJournal(writer);
                    }
                }
            }

            owner.Return();

            if (unloadMode.IsUnload())
            {// Unload
                crystal.Crystalizer.Memory.ReportUnloaded(this, dataSize);
                this.data = default;
            }
        }
        finally
        {
            this.Exit();
        }

        return true;
    }

    public void Erase()
    {
        this.EraseInternal();
        ((ITreeObject)this).AddJournalRecord(JournalRecord.EraseStorage);
    }

    #region Journal

    bool ITreeObject.ReadRecord(ref TinyhandReader reader)
    {
        if (!reader.TryPeek(out JournalRecord record))
        {
            return false;
        }

        if (record == JournalRecord.EraseStorage)
        {// Erase storage
            this.EraseInternal();
            return true;
        }
        else if (record == JournalRecord.AddStorage)
        {
            if (((ITreeObject)this).TreeRoot is not ICrystal crystal)
            {// No crystal
                return true;
            }

            reader.TryRead(out record);
            var storageId = TinyhandSerializer.DeserializeObject<StorageId>(ref reader);
            this.AddInternal(crystal, storageId);
            return true;
        }

        if (this.data is null)
        {
            this.data = this.Get().Result;
        }

        if (this.data is ITreeObject treeObject)
        {
            return treeObject.ReadRecord(ref reader);
        }
        else
        {
            return false;
        }
    }

    void ITreeObject.WriteLocator(ref TinyhandWriter writer)
    {
    }

    #endregion

    private async Task PrepareAndLoadInternal()
    {// using (this.Lock())
        if (this.data is not null)
        {
            return;
        }

        if (((ITreeObject)this).TreeRoot is not ICrystal crystal)
        {// No crystal
            return;
        }

        var storage = crystal.Storage;
        ulong fileId = 0;
        CrystalMemoryOwnerResult result;
        if (this.storageId0.IsValid)
        {
            fileId = this.storageId0.FileId;
            result = await storage.GetAsync(ref fileId).ConfigureAwait(false);
            if (result.IsFailure && this.storageId1.IsValid)
            {
                fileId = this.storageId1.FileId;
                result = await storage.GetAsync(ref fileId).ConfigureAwait(false);
                if (result.IsFailure && this.storageId2.IsValid)
                {
                    fileId = this.storageId2.FileId;
                    result = await storage.GetAsync(ref fileId).ConfigureAwait(false);
                    /*if (result.IsFailure && this.storageId3.IsValid)
                    {
                        fileId = this.storageId3.FileId;
                        result = await storage.GetAsync(ref fileId).ConfigureAwait(false);
                    }*/
                }
            }
        }
        else
        {
            result = new(CrystalResult.NotFound);
        }

        if (result.IsFailure)
        {
            return;
        }

        // Deserialize
        try
        {
            this.data = TinyhandSerializer.Deserialize<TData>(result.Data.Span);
            this.PrepareData(result.Data.Span.Length);
        }
        catch
        {
        }
        finally
        {
            result.Return();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrepareData(int dataSize)
    {
        if (this.data is ITreeObject treeObject)
        {
            treeObject.SetParent(this);
        }

        if (((ITreeObject)this).TreeRoot is ICrystal crystal)
        {
            crystal.Crystalizer.Memory.Register(this, dataSize);
        }
    }

    private void AddInternal(ICrystal crystal, StorageId storageId)
    {
        var numberOfHistories = crystal.CrystalConfiguration.NumberOfFileHistories;
        ulong fileId;
        var storage = crystal.Storage;

        if (numberOfHistories <= 1)
        {
            this.storageId0 = storageId;
        }
        else if (numberOfHistories == 2)
        {
            if (this.storageId1.IsValid)
            {
                fileId = this.storageId1.FileId;
                storage.DeleteAndForget(ref fileId);
            }

            this.storageId1 = this.storageId0;
            this.storageId0 = storageId;
        }
        else
        {
            if (this.storageId2.IsValid)
            {
                fileId = this.storageId2.FileId;
                storage.DeleteAndForget(ref fileId);
            }

            this.storageId2 = this.storageId1;
            this.storageId1 = this.storageId0;
            this.storageId0 = storageId;
        }

        /*else
        {
            if (this.storageId3.IsValid)
            {
                fileId = this.storageId3.FileId;
                storage.DeleteAndForget(ref fileId);
            }

            this.storageId3 = this.storageId2;
            this.storageId2 = this.storageId1;
            this.storageId1 = this.storageId0;
            this.storageId0 = storageId;
        }*/
    }

    private void EraseInternal()
    {
        ITreeObject? treeObject;
        ulong id0;
        ulong id1;
        ulong id2;
        // ulong id3;

        using (this.Lock())
        {
            treeObject = this.data as ITreeObject;

            id0 = this.storageId0.FileId;
            id1 = this.storageId1.FileId;
            id2 = this.storageId2.FileId;
            // id3 = this.storageId3.FileId;

            this.data = default;
            this.storageId0 = default;
            this.storageId1 = default;
            this.storageId2 = default;
            // this.storageId3 = default;
        }

        if (((ITreeObject)this).TreeRoot is ICrystal crystal)
        {// Delete storage
            var storage = crystal.Storage;

            if (id0 != 0)
            {
                storage.DeleteAndForget(ref id0);
            }

            if (id1 != 0)
            {
                storage.DeleteAndForget(ref id1);
            }

            if (id2 != 0)
            {
                storage.DeleteAndForget(ref id2);
            }

            /*if (id3 != 0)
            {
                storage.DeleteAndForget(ref id3);
            }*/
        }

        if (treeObject is not null)
        {
            treeObject.Erase();
        }
    }
}
