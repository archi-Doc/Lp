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
public partial record DesignRepeatable
{
    public DesignRepeatable()
    {
    }

    [Key(0)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    [Key(1)]
    public DesignRepeatable Class { get; set; } = new();

    [Key(2)]
    public UnloadableData<DesignRepeatable> UnloadableClass { get; set; } = new();
}

/// <summary>
/// <see cref="UnloadableData{TData}"/> is a subset of <see cref="CrystalObject{TData}"/>, allowing for the persistence of partial data.
/// </summary>
/// <typeparam name="TData">The type of data.</typeparam>
[TinyhandObject(ExplicitKeyOnly = true)]
public sealed partial class UnloadableData<TData> : SemaphoreLock, ITreeObject
// where TData : ITinyhandSerialize<TData>
{
    #region FieldAndProperty

    [Key(0)]
    private StorageConfiguration? storageConfiguration; // using (this.Lock())

    [Key(1)]
    private StoragePoint storagePoint; // using (this.Lock())

    [Key(2)]
    private TData? data; // using (this.Lock())

    private bool dataChanged;

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
                this.dataChanged = true;
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
            this.dataChanged = true;
        }
    }

    public void SetStorageConfiguration(StorageConfiguration storageConfiguration)
    {
        using (this.Lock())
        {
            this.storageConfiguration = storageConfiguration;
            this.dataChanged = true;
        }
    }

    public async Task<bool> Save(UnloadMode unloadMode)
    {
        using (this.Lock())
        {
            if (this.data is null)
            {// No data
                return true;
            }
            else if (!this.dataChanged)
            {// No change
                goto Exit;
            }
            else
            {
                this.dataChanged = false;
            }

            var treeObject = this.data as ITreeObject;
            if (treeObject is not null)
            {
                var result = await treeObject.Save(unloadMode).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
            }

            var bin = TinyhandSerializer.Serialize<TData>(this.data, TinyhandSerializerOptions.Unload);

Exit:
            if (unloadMode.IsUnload())
            {// Unload
            }
        }

        return true;
    }

    public void Delete()
    {
        ITreeObject? treeObject;

        using (this.Lock())
        {
            treeObject = this.data as ITreeObject;

            this.storagePoint = default;
            this.data = default;
            this.dataChanged = true;
        }

        Crystalizer crystalizer = default!;
        var storage = crystalizer.ResolveStorage(this.storageConfiguration!);
        var storageId = 123ul;
        storage.DeleteAndForget(ref storageId);

        if (treeObject is not null)
        {
            treeObject.Delete();
        }
    }

    public void NotifyDataChanged()
    {
        // using (this.Lock())
        {
            this.dataChanged = true;
        }
    }

    /*
    #region ITinyhandSerialize

    static void ITinyhandSerialize<UnloadableData<TData>>.Serialize(ref TinyhandWriter writer, scoped ref UnloadableData<TData>? v, TinyhandSerializerOptions options)
    {
        if (v == null)
        {
            writer.WriteNil();
            return;
        }

        if (!options.IsSignatureMode)
        {
            writer.WriteArrayHeader(3);
        }

        if (v.storageConfiguration is null)
        {
            writer.WriteNil();
        }
        else
        {
            TinyhandSerializer.SerializeObject(ref writer, v.storageConfiguration, options);
        }

        TinyhandSerializer.SerializeObject(ref writer, v.storagePoint, options);
        TinyhandSerializer.Serialize(ref writer, v.data, options);
    }

    static unsafe void ITinyhandSerialize<UnloadableData<TData>>.Deserialize(ref TinyhandReader reader, scoped ref UnloadableData<TData>? v, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var numberOfData = reader.ReadArrayHeader();
        options.Security.DepthStep(ref reader);
        try
        {
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {

                ulong vd;
                vd = reader.ReadUInt64();
                fixed (ulong* ptr = &v.JournalPosition) *ptr = vd;
            }
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                ulong vd;
                vd = reader.ReadUInt64();
                fixed (ulong* ptr = &v.Hash) *ptr = vd;
            }

            while (numberOfData-- > 0)
            {
                reader.Skip();
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    #endregion
    */

    #region ITreeObject

    [IgnoreMember]
    ITreeRoot? ITreeObject.TreeRoot { get; set; }

    [IgnoreMember]
    ITreeObject? ITreeObject.TreeParent { get; set; }

    [IgnoreMember]
    int ITreeObject.TreeKey { get; set; }

    #endregion

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
            this.dataChanged = false;
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

    private void Unload(bool delete)
    {
    }
}
