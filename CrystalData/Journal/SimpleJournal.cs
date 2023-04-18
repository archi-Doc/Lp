// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CrystalData.Filer;
using Tinyhand.IO;

namespace CrystalData.Journal;

public partial class SimpleJournal : IJournal
{
    public const int MaxRecordLength = 1024 * 16; // 16 KB
    public const int MaxBookLength = 1024 * 1024 * 16; // 16 MB
    public const int MaxMemoryLength = 1024 * 1024 * 64; // 64 MB
    public const string BookSuffix = ".book";
    private const int InitialBufferSize = 32 * 1024; // 32 KB

    [ThreadStatic]
    private static byte[]? initialBuffer;

    public SimpleJournal(Crystalizer crystalizer, SimpleJournalConfiguration configuration)
    {
        this.crystalizer = crystalizer;
        this.SimpleJournalConfiguration = configuration;
    }

    #region PropertyAndField

    public SimpleJournalConfiguration SimpleJournalConfiguration { get; }

    private Crystalizer crystalizer;
    private bool prepared;
    private IRawFiler? rawFiler;

    // Token
    private ConcurrentDictionary<uint, IJournalObject> tokenToObjects = new();

    // Journal
    private object syncJournal = new();
    private SimpleJournalBook.GoshujinClass books = new();
    private ulong memoryUsage;

    #endregion

    public async Task<CrystalResult> Prepare(PrepareParam param)
    {
        if (this.prepared)
        {
            return CrystalResult.Success;
        }

        var configuration = this.SimpleJournalConfiguration.DirectoryConfiguration;

        this.rawFiler ??= this.crystalizer.ResolveRawFiler(configuration);
        var result = await this.rawFiler.PrepareAndCheck(this.crystalizer, configuration).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return result;
        }

        // List journal books
        var list = await this.rawFiler.ListAsync(configuration.Path).ConfigureAwait(false);

        this.prepared = true;
        return CrystalResult.Success;
    }

    void IJournal.GetWriter(JournalRecordType recordType, uint token, out TinyhandWriter writer)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[InitialBufferSize];
        }

        writer = new(initialBuffer);
        writer.Advance(3); // Size (0-16MB)
        writer.RawWriteUInt8(Unsafe.As<JournalRecordType, byte>(ref recordType)); // JournalRecordType
        writer.RawWriteUInt32(token); // JournalToken
    }

    ulong IJournal.Add(in TinyhandWriter writer)
    {
        writer.FlushAndGetMemory(out var memory, out var useInitialBuffer);
        writer.Dispose();

        if (memory.Length > MaxRecordLength)
        {
            throw new InvalidOperationException($"The maximum length per record is {MaxRecordLength} bytes.");
        }

        // Size (0-16MB)
        var span = memory.Span;
        span[2] = (byte)memory.Length;
        span[1] = (byte)(memory.Length >> 8);
        span[0] = (byte)(memory.Length >> 16);

        lock (this.syncJournal)
        {
            SimpleJournalBook book = this.EnsureBook();
            var waypoint = book.AppendData(memory.Span);
        }

        return 0;
    }

    uint IJournal.NewToken(IJournalObject journalObject)
    {
        while (true)
        {
            var token = RandomVault.Pseudo.NextUInt32();
            if (token != 0 && this.tokenToObjects.TryAdd(token, journalObject))
            {// Success
                return token;
            }
        }
    }

    bool IJournal.RegisterToken(uint token, IJournalObject journalObject)
    {
        if (token == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(token));
        }

        return this.tokenToObjects.TryAdd(token, journalObject);
    }

    uint IJournal.UpdateToken(uint oldToken, IJournalObject journalObject)
    {
        if (oldToken != 0)
        {
            this.tokenToObjects.TryRemove(oldToken, out _);
        }

        return ((IJournal)this).NewToken(journalObject);
    }

    bool IJournal.UnregisterToken(uint token)
    {
        if (token == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(token));
        }

        return this.tokenToObjects.TryRemove(token, out _);
    }

    private SimpleJournalBook EnsureBook()
    {// lock (this.syncJournal)
        SimpleJournalBook book;

        if (this.books.Count == 0)
        {// Empty
            book = SimpleJournalBook.AppendNewBook(this, null);
        }
        else
        {
            book = this.books.BookStartChain.Last!;
            book.EnsureBuffer(MaxRecordLength);
        }

        return book;
    }
}
