// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using CrystalData.Filer;
using Tinyhand.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CrystalData.Journal;

public partial class SimpleJournal : IJournal
{
    public const string FinishedSuffix = ".finished";
    public const string UnfinishedSuffix = ".unfinished";
    public const int RecordBufferLength = 1024 * 1024 * 1; // 1MB
    private const int MergeThresholdNumber = 100;
    private const int MergeThresholdLength = 100;

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

    // Temporary buffer
    private object syncRecordBuffer = new(); // syncRecordBuffer -> syncBooks
    private byte[] recordBuffer = new byte[RecordBufferLength];
    private ulong recordBufferPosition = 0; // JournalPosition
    private int recordBufferLength = 0;

    private int recordBufferRemaining => RecordBufferLength - this.recordBufferLength;

    // Books
    private object syncBooks = new();
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

        lock (this.syncRecordBuffer)
        {
            if (this.recordBufferRemaining < span.Length)
            {
                this.FlushRecordBufferInternal();
            }

            span.CopyTo(this.recordBuffer.AsSpan(this.recordBufferLength));
            this.recordBufferLength += span.Length;
            return this.recordBufferPosition + (ulong)this.recordBufferLength;
        }
    }

    public async Task<bool> ReadJournalAsync(ulong start, ulong end, Memory<byte> data)
    {
        lock (this.books)
        {
            this.books.PositionChain.GetRange(start, end - 1);
        }
    }

    internal void FlushRecordBuffer()
    {
        lock (this.syncRecordBuffer)
        {
            this.FlushRecordBufferInternal();
        }
    }

    internal async Task MergeInternal()
    {// lock (this.syncBooks)
        var book = this.books.UnfinishedChain.First;
        var unfinishedCount = 0;
        var unfinishedLength = 0;
        Book? lastBook = null; // The last book to be merged.
        var lastLength = 0;
        while (book != null)
        {
            unfinishedCount++;
            unfinishedLength += book.Length;
            if (unfinishedLength <= this.SimpleJournalConfiguration.FinishedBookLength)
            {
                lastBook = book;
                lastLength = unfinishedLength;
            }
        }

        if (unfinishedCount < MergeThresholdNumber ||
            unfinishedLength < this.SimpleJournalConfiguration.FinishedBookLength)
        {
            return;
        }

        var start = this.books.UnfinishedChain.First!.PositionValue;
        var end = start + (ulong)lastLength;

        // Exit

        var buffer = ArrayPool<byte>.Shared.Rent(lastLength);
        if (!await this.ReadJournalAsync(start, end, buffer).ConfigureAwait(false))
        {
            return;
        }

    }

    private void FlushRecordBufferInternal()
    {// lock (this.syncRecordBuffer)
        var book = Book.AppendNewBook(this, this.recordBufferPosition, this.recordBuffer, this.recordBufferLength);

        this.recordBufferPosition += (ulong)this.recordBufferLength;
        this.recordBufferLength = 0;
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

        lock (this.syncBooks)
        {
            this.books.Clear();

            foreach (var x in list)
            {
                var book = Book.TryAdd(this, x);
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

            nextPosition = book.PositionValue + (ulong)book.Length;
            previous = book;
        }

        if (toDelete == null)
        {// Ok
            return;
        }

        while (true)
        {// Delete books that have lost consistency.
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
