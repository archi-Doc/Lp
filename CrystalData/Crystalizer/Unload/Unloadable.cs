// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData.Unload;

[TinyhandObject(Journal = true)]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class DesignClass
{
    public DesignClass()
    {
        this.UnloadableClass = new(this);
    }

    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public DesignClass Class { get; set; } = new();

    [Key(2)]
    public Unloadable<DesignClass> UnloadableClass { get; set; }
}

[TinyhandObject]
public sealed partial class Unloadable<TData> : ITinyhandSerialize<Unloadable<TData>>, IJournalObject
// where TData : ITinyhandSerialize<TData>
{
    public Unloadable()
    {
    }

    public Unloadable(IJournalObject parent)
    {
        this.parent = parent;
    }

    static void ITinyhandSerialize<Unloadable<TData>>.Serialize(ref TinyhandWriter writer, scoped ref Unloadable<TData>? v, TinyhandSerializerOptions options)
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

        writer.Write(v.storage);
        if (v.fileConfiguration is null)
        {
            writer.WriteNil();
        }
        else
        {
            TinyhandSerializer.SerializeObject(ref writer, v.fileConfiguration, options);
        }
    }

    static void ITinyhandSerialize<Unloadable<TData>>.Deserialize(ref TinyhandReader reader, scoped ref Unloadable<TData>? value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    #region FieldAndProperty

    private readonly IJournalObject parent;
    private TData? data;

    private int storage;
    private FileConfiguration? fileConfiguration;

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

/*public struct Unloadable<TData>
    where TData : ITinyhandSerialize<TData>
{
    public Unloadable(BaseData baseData)
    {
        this.baseData = baseData;
    }

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

    public bool TryUnload()
    {
        var bin = TinyhandSerializer.Serialize<TData>(Data, TinyhandSerializerOptions.Unload);
        return true;
    }

    private readonly BaseData baseData;
    private TData? data;
}*/
