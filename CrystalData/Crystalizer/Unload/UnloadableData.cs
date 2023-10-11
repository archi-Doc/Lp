// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace CrystalData;

[TinyhandObject(Journal = true)]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class DesignClass
{
    public DesignClass()
    {
    }

    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public DesignClass Class { get; set; } = new();

    [Key(2)]
    public UnloadableData<DesignClass> UnloadableClass { get; set; } = new();
}

/// <summary>
/// <see cref="UnloadableData{TData}"/> is a subset of <see cref="CrystalObject{TData}"/>, allowing for the persistence of partial data.
/// </summary>
/// <typeparam name="TData">The type of data.</typeparam>
[TinyhandObject]
public sealed partial class UnloadableData<TData> : ITinyhandSerialize<UnloadableData<TData>>, ITreeObject
// where TData : ITinyhandSerialize<TData>
{
    #region FieldAndProperty

    private StorageConfiguration? storageConfiguration;
    private StoragePoint storagePoint;
    private TData? data;
    private bool dataChanged;

    private object syncObject => this;

    public TData Data
    {
        get
        {
            if (this.data is { } data)
            {
                return data;
            }

            // lock (baseData.SemaphoreLock)
            {

            }

            return TinyhandSerializer.Reconstruct<TData>();
        }
    }

    #endregion

    public UnloadableData()
    {
    }

    public async Task<bool> Save(UnloadMode unloadMode)
    {
        if (!Volatile.Read(ref this.dataChanged))
        {// No change
            if (unloadMode.IsUnload())
            {// Unload
            }

            return true;
        }

        var treeObject = this.Data as ITreeObject;
        if (treeObject is not null)
        {
            var result = await treeObject.Save(unloadMode);
            if (!result)
            {
                return false;
            }
        }

        var bin = TinyhandSerializer.Serialize<TData>(this.Data, TinyhandSerializerOptions.Unload);
    }

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

    static void ITinyhandSerialize<UnloadableData<TData>>.Deserialize(ref TinyhandReader reader, scoped ref UnloadableData<TData>? value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region ITreeObject

    [IgnoreMember]
    ITreeRoot? ITreeObject.TreeRoot { get; set; }

    [IgnoreMember]
    ITreeObject? ITreeObject.TreeParent { get; set; }

    [IgnoreMember]
    int ITreeObject.TreeKey { get; set; }

    void ITreeObject.NotifyDataChanged()
    {
        Volatile.Write(ref this.dataChanged, true);
    }

    #endregion
}
