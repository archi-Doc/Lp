// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;
using Tinyhand.IO;

namespace CrystalData;

/// <summary>
/// <see cref="DatumObject"/> holds a datum id, storage/file ids, and an instance of the datum.
/// </summary>
[TinyhandObject]
public partial struct DatumObject : ITinyhandSerialize<DatumObject>
{
    public DatumObject()
    {
    }

    public static void Serialize(ref TinyhandWriter writer, scoped ref DatumObject value, TinyhandSerializerOptions options)
    {
        writer.Write(value.DatumId);
        writer.Write(value.StorageId);
        writer.Write(value.FileId);
        writer.Write(value.FileChecksum);
    }

    public static void Deserialize(ref TinyhandReader reader, scoped ref DatumObject value, TinyhandSerializerOptions options)
    {
        value.DatumId = reader.ReadUInt16();
        value.StorageId = reader.ReadUInt16();
        value.FileId = reader.ReadUInt64();
        value.FileChecksum = reader.ReadUInt64();
    }

    [Key(0)]
    internal ushort DatumId;

    [Key(1)]
    internal ushort StorageId;

    [Key(2)]
    internal ulong FileId;

    [Key(3)]
    internal ulong FileChecksum;

    internal bool IsValid => this.DatumId != 0;

    internal bool IsValidStorage => this.StorageId != 0;

    internal IBaseDatum? Datum;
}
