// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData.Unload;

public class Unloadable<TData> : IUnloadable, ITinyhandSerialize<Unloadable<TData>>
    where TData : ITinyhandSerialize<TData>
{
    public Unloadable(BaseData baseData)
    {
        this.baseData = baseData;
    }

    static void ITinyhandSerialize<Unloadable<TData>>.Serialize(ref TinyhandWriter writer, scoped ref Unloadable<TData>? value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    static void ITinyhandSerialize<Unloadable<TData>>.Deserialize(ref TinyhandReader reader, scoped ref Unloadable<TData>? value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
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

    public void Save()
    {
        throw new NotImplementedException();
    }

    public void Unload()
    {
        this.data = default;
    }

    private readonly BaseData baseData;
    private TData? data;
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
