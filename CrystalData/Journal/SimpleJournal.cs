// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData.Filer;
using Tinyhand.IO;

namespace CrystalData.Journal;

public partial class SimpleJournal : IJournal
{
    public const string FinishedSuffix = ".finished";
    public const string UnfinishedSuffix = ".unfinished";
    public const int PositionLength = sizeof(ulong);

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

    // Journal
    private object syncObject = new();
    private Book.GoshujinClass books = new();
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
        var result = await this.rawFiler.PrepareAndCheck(param, configuration).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return result;
        }

        // List journal books
        await this.ListBooks();

        this.prepared = true;
        return CrystalResult.Success;
    }

    void IJournal.GetWriter(JournalRecordType recordType, uint plane, out TinyhandWriter writer)
    {
        if (initialBuffer == null)
        {
            initialBuffer = new byte[this.SimpleJournalConfiguration.MaxRecordLength];
        }

        writer = new(initialBuffer);
        writer.Advance(3); // Size(0-16MB): byte[3]
        writer.RawWriteUInt8(Unsafe.As<JournalRecordType, byte>(ref recordType)); // JournalRecordType: byte
        writer.RawWriteUInt32(plane); // Plane: byte[4]
    }

    ulong IJournal.Add(in TinyhandWriter writer)
    {
        writer.FlushAndGetMemory(out var memory, out var useInitialBuffer);
        writer.Dispose();

        if (memory.Length > this.SimpleJournalConfiguration.MaxRecordLength)
        {
            throw new InvalidOperationException($"The maximum length per record is {this.SimpleJournalConfiguration.MaxRecordLength} bytes.");
        }

        // Size (0-16MB)
        var span = memory.Span;
        span[2] = (byte)memory.Length;
        span[1] = (byte)(memory.Length >> 8);
        span[0] = (byte)(memory.Length >> 16);

        lock (this.syncObject)
        {
            Book book = this.EnsureBook();
            var position = book.AppendData(memory.Span);
            return position;
        }
    }

    private Book EnsureBook()
    {// lock (this.syncJournal)
        Book book;

        if (this.books.Count == 0)
        {// Empty
            book = Book.AppendNewBook(this, null);
        }
        else
        {
            book = this.books.BookStartChain.Last!;
            book.EnsureBuffer(this.SimpleJournalConfiguration.MaxRecordLength);
        }

        return book;
    }

    private async Task ListBooks()
    {
        if (this.rawFiler == null)
        {
            return;
        }

        var list = await this.rawFiler.ListAsync(this.SimpleJournalConfiguration.DirectoryConfiguration.Path).ConfigureAwait(false);
        if (list == null)
        {
            return;
        }

        lock (this.syncObject)
        {
            this.books.Clear();

            foreach (var x in list)
            {
                Book.TryAdd(this, x);
            }

            this.CheckBooksInternal();
        }
    }

    private void CheckBooksInternal()
    {// lock (this.syncObject)
        ulong nextPosition = 0;
        Book? previous = null;
        Book? toDelete = null;

        foreach (var book in this.books)
        {
            if (previous != null && book.PositionValue != nextPosition)
            {
                toDelete = previous;
            }

            nextPosition = book.PositionValue + book.Length;
            previous = book;
        }

        if (toDelete == null)
        {// Ok
            return;
        }

        while (true)
        {
            var first = this.books.PositionChain.First;
            if (first == null)
            {
                return;
            }

            first.DeleteInternal();
            if (first == toDelete)
            {
                return;
            }
        }
    }
}
