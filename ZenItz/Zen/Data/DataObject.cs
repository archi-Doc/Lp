// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace ZenItz;

[TinyhandObject]
internal partial struct DataObject : ITinyhandSerialize<DataObject>
{
    public DataObject()
    {
    }

    public static void Serialize(ref TinyhandWriter writer, scoped ref DataObject value, TinyhandSerializerOptions options)
    {
        writer.Write(value.Id);
        writer.Write(value.File);
    }

    public static void Deserialize(ref TinyhandReader reader, scoped ref DataObject value, TinyhandSerializerOptions options)
    {
        value.Id = reader.ReadInt32();
        value.File = reader.ReadUInt64();
    }

    [Key(0)]
    internal int Id;

    [Key(1)]
    internal ulong File;

    internal bool IsValid => this.Id != 0;

    internal BaseData? Data;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal (object? Data, bool Created) GetOrCreateObject(IFlakeInternal flakeInternal)
    {
        if (this.Data == null)
        {
            var construtor = ZenData.TryGetConstructor(this.Id);
            if (construtor != null)
            {
                this.Data = construtor(flakeInternal);
            }

            return (this.Data, this.Data != null);
        }

        return (this.Data, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Save()
    {
        this.Data?.Save();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Unload()
    {
        this.Data?.Unload();
        this.Data = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Remove(ZenIO io)
    {
        this.Data?.Unload();
        this.Data = null;
        io.Remove(this.File);
        this.File = 0;
    }
}
