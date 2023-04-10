// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using CrystalData.Filer;
using Tinyhand.IO;

namespace CrystalData.Journal;

public class SimpleJournal : IJournalInternal
{
    public const int MaxRecordSize = 1024 * 16; // 16 KB
    public const int BookLength = 1024 * 1024 * 16; // 16 MB

    public const int RecordBufferLength = 1024 * 4; // 4 KB
    public const int RecordBufferNumber = 32;

    [ThreadStatic]
    private static byte[] initialBuffer = new byte[1024 * 16]; // 16 KB

    public SimpleJournal(Crystalizer crystalizer, SimpleJournalConfiguration configuration)
    {
        this.crystalizer = crystalizer;
        this.SimpleJournalConfiguration = configuration;

        // this.bufferPool = ArrayPool<byte>.Create(RecordBufferLength, RecordBufferNumber);
    }

    public async Task<CrystalStartResult> Prepare()
    {
        this.rawFiler ??= this.crystalizer.ResolveRawFiler(this.SimpleJournalConfiguration.DirectoryConfiguration);

        return CrystalStartResult.Success;
    }

    void IJournal.GetJournalWriter(JournalRecordType recordType, out TinyhandWriter writer)
    {
        writer = new(initialBuffer);
        writer.Write(Unsafe.As<JournalRecordType, byte>(ref recordType));

        /*var buffer = this.bufferPool.Rent(RecordBufferLength);
        record = new(this, buffer);
        record.Writer.Write(Unsafe.As<JournalRecordType, byte>(ref recordType));}*/
    }

    ulong IJournal.AddRecord(in TinyhandWriter writer)
    {
        writer.FlushAndGetMemory(out var memory, out var useInitialBuffer);
        writer.Dispose();

        lock (this.syncJournal)
        {
        }

        return 0;

        /*record.Writer.FlushAndGetMemory(out var memory, out _);
        record.Writer.Dispose();

        lock (this.syncJournal)
        {
        }

        this.bufferPool.Return(record.Buffer);
        return 0;*/
    }

    public SimpleJournalConfiguration SimpleJournalConfiguration { get; }

    public bool Prepared { get; private set; }

    private Crystalizer crystalizer;
    private IRawFiler? rawFiler;

    // private ArrayPool<byte> bufferPool;

    // Journal
    private object syncJournal = new();
    private SimpleJournalBook.GoshujinClass books = new();
}
