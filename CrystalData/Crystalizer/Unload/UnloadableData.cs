// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData;

[TinyhandObject(Journal = true)]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class DesignClass
{
    public DesignClass()
    {
        this.UnloadableClass = new();
    }

    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public DesignClass Class { get; set; } = new();

    [Key(2)]
    public UnloadableData<DesignClass> UnloadableClass { get; set; }
}

/// <summary>
/// <see cref="UnloadableData{TData}"/> is a subset of <see cref="CrystalObject{TData}"/>, allowing for the persistence of partial data.
/// </summary>
/// <typeparam name="TData">The type of data.</typeparam>
[TinyhandObject]
public sealed partial class UnloadableData<TData> : ITinyhandSerialize<UnloadableData<TData>>, IJournalObject
// where TData : ITinyhandSerialize<TData>
{
    public UnloadableData()
    {
    }

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

    #region FieldAndProperty

    private StorageConfiguration? storageConfiguration;
    private StoragePoint storagePoint;
    private TData? data;

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

            return default!;
        }
    }

    #endregion

    #region IJournalObject

    [IgnoreMember]
    ITinyhandJournal? IJournalObject.Journal { get; set; }

    [IgnoreMember]
    IJournalObject? IJournalObject.JournalParent { get; set; }

    [IgnoreMember]
    int IJournalObject.JournalKey { get; set; }

    #endregion

    public bool TryUnload()
    {
        var bin = TinyhandSerializer.Serialize<TData>(Data, TinyhandSerializerOptions.Unload);
        return true;
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public void Unload()
    {
        this.data = default;
    }
}
