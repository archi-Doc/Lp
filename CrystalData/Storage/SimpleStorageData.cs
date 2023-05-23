// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace CrystalData.Storage;

[TinyhandObject(Journaling = true)]
internal partial class SimpleStorageData : ITinyhandSerialize<SimpleStorageData>, ITinyhandCustomJournal
{
    public SimpleStorageData()
    {
    }

    #region PropertyAndField

    public long StorageUsage => this.storageUsage;

    private object syncObject = new();
    private long storageUsage; // syncObject
    private Dictionary<uint, int> fileToSize = new(); // syncObject

    #endregion

    static void ITinyhandSerialize<SimpleStorageData>.Serialize(ref TinyhandWriter writer, scoped ref SimpleStorageData? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        lock (value.syncObject)
        {
            writer.Write(value.storageUsage);

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
            value.storageUsage = reader.ReadInt64();

            var count = reader.ReadMapHeader();
            value.fileToSize = new(count);
            for (var i = 0; i < count; i++)
            {
                value.fileToSize.TryAdd(reader.ReadUInt32(), reader.ReadInt32());
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(uint file)
    {
        lock (this.syncObject)
        {
            if (this.Crystal?.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer) == true)
            {
                writer.Write_Remove();
                writer.Write(file);
                this.Crystal.AddJournal(writer);
            }

            return this.fileToSize.Remove(file);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(uint file, out int size)
    {
        lock (this.syncObject)
        {
            return this.fileToSize.TryGetValue(file, out size);
        }
    }

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint NewFile(int size)
    {
        lock (this.syncObject)
        {
            return this.NewFileInternal(size);
        }
    }*/

    public void Put(ref uint file, int dataSize)
    {
        lock (this.syncObject)
        {
            if (file != 0 && this.fileToSize.TryGetValue(file, out var size))
            {
                var sizeDiff = dataSize - size;
                this.storageUsage += sizeDiff;

                this.fileToSize[file] = dataSize;

                if (sizeDiff != 0 && this.Crystal?.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer) == true)
                {
                    writer.Write_Add();
                    writer.Write(file);
                    writer.Write(dataSize);
                    writer.Write(sizeDiff);
                    this.Crystal.AddJournal(writer);
                }
            }
            else
            {// Not found
                file = this.NewFileInternal(dataSize);
                this.storageUsage += dataSize;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint NewFileInternal(int size)
    {// this.syncObject
        while (true)
        {
            var file = RandomVault.Pseudo.NextUInt32();
            if (this.fileToSize.TryAdd(file, size))
            {
                if (this.Crystal?.TryGetJournalWriter(JournalType.Record, this.CurrentPlane, out var writer) == true)
                {
                    writer.Write_Add();
                    writer.Write(file);
                    writer.Write(size);
                    writer.Write(size);
                    this.Crystal.AddJournal(writer);
                }

                return file;
            }
        }
    }

    void ITinyhandCustomJournal.WriteCustomLocator(ref TinyhandWriter writer)
    {
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        var record = reader.Read_Record();
        if (record == JournalRecord.Add)
        {
            var file = reader.ReadUInt32();
            var size = reader.ReadInt32();
            var diff = reader.ReadInt32();
            this.fileToSize[file] = size;
            this.storageUsage += diff;

            return true;
        }
        else if (record == JournalRecord.Remove)
        {
            var file = reader.ReadUInt32();
            this.fileToSize.Remove(file);

            return true;
        }

        return false;
    }
}
