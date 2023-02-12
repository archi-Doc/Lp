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

    [Key(0)]
    internal int Id;

    [Key(1)]
    internal ulong File;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal (BaseData? Data, bool Created) GetOrCreateObject(ZenOptions options)
    {
        if (this.@object == null)
        {
            this.@object = ZenData.TryCreateInstance(this.Id, options);
            return (this.@object, this.@object != null);
        }

        return (this.@object, false);
    }

    private BaseData? @object;

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
}
