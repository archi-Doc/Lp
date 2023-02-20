// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData;

/// <summary>
/// <see cref="DatumObject"/> holds a datum id, file, and an instance of the datum.
/// </summary>
[TinyhandObject]
internal partial struct DatumObject : ITinyhandSerialize<DatumObject>
{
    public DatumObject()
    {
    }

    public static void Serialize(ref TinyhandWriter writer, scoped ref DatumObject value, TinyhandSerializerOptions options)
    {
        writer.Write(value.Id);
        writer.Write(value.File);
    }

    public static void Deserialize(ref TinyhandReader reader, scoped ref DatumObject value, TinyhandSerializerOptions options)
    {
        value.Id = reader.ReadInt32();
        value.File = reader.ReadUInt64();
    }

    [Key(0)]
    internal int Id;

    [Key(1)]
    internal ulong File;

    internal bool IsValid => this.Id != 0;

    internal IBaseDatum? Data;
}
