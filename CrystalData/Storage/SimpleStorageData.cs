// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace CrystalData.Storage;

[TinyhandObject]
internal partial class SimpleStorageData : ITinyhandSerialize<SimpleStorageData>, IJournalObject
{
    public SimpleStorageData()
    {
    }

    private object syncObject = new();
    private Dictionary<uint, int> fileToSize = new();

    static void ITinyhandSerialize<SimpleStorageData>.Serialize(ref TinyhandWriter writer, scoped ref SimpleStorageData? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        lock (value.syncObject)
        {
            writer.WriteMapHeader(value.fileToSize.Count);
            foreach (var x in value.fileToSize)
            {
                writer.Write(x.Key);
                writer.Write(x.Value);
            }
        }
    }

    static void ITinyhandSerialize<SimpleStorageData>.Deserialize(ref TinyhandReader reader, scoped ref SimpleStorageData? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        value ??= new();
        lock (value.syncObject)
        {
            var count = reader.ReadMapHeader();
            value.fileToSize = new(count);
            for (var i = 0; i < count; i++)
            {
                value.fileToSize.TryAdd(reader.ReadUInt32(), reader.ReadInt32());
            }
        }
    }
}
