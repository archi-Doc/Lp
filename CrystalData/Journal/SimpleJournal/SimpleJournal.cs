// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CrystalData.Filer;
using Tinyhand.IO;

namespace CrystalData.Journal;

public partial class SimpleJournal : IJournal
{
    public const string FinishedSuffix = ".finished";
    public const string UnfinishedSuffix = ".unfinished";
    public const int RecordBufferLength = 1024 * 1024 * 1; // 1MB
    private const int MergeThresholdNumber = 100;

    [ThreadStatic]
    private static byte[]? initialBuffer;

    public SimpleJournal(Crystalizer crystalizer, SimpleJournalConfiguration configuration, ILogger<SimpleJournal> logger)
    {
        this.crystalizer = crystalizer;
        this.SimpleJournalConfiguration = configuration;
        this.logger = logger;
    }

    #region PropertyAndField

    public SimpleJournalConfiguration SimpleJournalConfiguration { get; }

    private Crystalizer crystalizer;
    private bool prepared;
    private IRawFiler? rawFiler;
    private SimpleJournalTask? task;

    // Record buffer
    private object syncRecordBuffer = new(); // syncRecordBuffer -> syncBooks
    private byte[] recordBuffer = new byte[RecordBufferLength];
    private ulong recordBufferPosition = 1; // JournalPosition
    private int recordBufferLength = 0;

    private int recordBufferRemaining => RecordBufferLength - this.recordBufferLength;

    // Books
    private object syncBooks = new();
    private Book.GoshujinClass books = new();
    private int memoryUsage;
    private ulong unfinishedSize;

    internal ILogger logger { get; private set; }

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
        await this.ListBooks().ConfigureAwait(false);

        this.task ??= new(this);

        this.logger.TryGet()?.Log($"Prepared: {this.books.PositionChain.First?.Position} - {this.books.PositionChain.Last?.Position} ({this.books.PositionChain.Count})");

        await this.Merge(false).ConfigureAwait(false);

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

    async Task IJournal.SaveJournalAsync()
    {
        await this.SaveJournalAsync(true).ConfigureAwait(false);
    }

    async Task IJournal.TerminateAsync()
    {
        if (this.task is { } task)
        {
            task.Terminate();
            await task.WaitForTerminationAsync(-1).ConfigureAwait(false);
        }

        lock (this.syncBooks)
        {
            var array = this.books.ToArray();
            foreach (var x in array)
            {
                x.Goshujin = null;
            }
        }

        // Terminate
        this.logger.TryGet()?.Log($"Terminated - {this.memoryUsage}");
    }

    public async Task<bool> ReadJournalAsync(ulong start, ulong end, Memory<byte> data)
    {
        var length = (int)(end - start);
        if (data.Length < length)
        {
            return false;
        }

        var retry = 0;
        List<(ulong Position, string Path)> loadList = new();

Load:
        if (retry++ >= 2 || this.rawFiler == null)
        {
            return false;
        }

        foreach (var x in loadList)
        {
            var result = await this.rawFiler.ReadAsync(x.Path, 0, -1).ConfigureAwait(false);
            if (result.IsFailure)
            {
                return false;
            }

            try
            {
                lock (this.syncBooks)
                {
                    var book = this.books.PositionChain.FindFirst(x.Position);
                    if (book is not null)
                    {
                        book.TrySetBuffer(result.Data);
                    }
                }
            }
            finally
            {
                result.Return();
            }
        }

        lock (this.syncBooks)
        {
            var range = this.books.PositionChain.GetRange(start, end - 1);
            if (range.Upper is null)
            {
                return false;
            }
            else if (range.Lower is null)
            {
                range.Lower = range.Upper.PositionLink.Previous ?? range.Upper;
            }

            if (start < range.Lower.Position)
            {
                return false;
            }

            // range.Lower.Position <= start, range.Upper.Position < end

            // Check
            loadList.Clear();
            for (var book = range.Lower; book != null; book = book.PositionLink.Next)
            {
                if (!book.IsInMemory)
                {// Load (start, path)
                    if (book.Path is not null)
                    {
                        loadList.Add((book.Position, book.Path));
                    }
                }

                if (book == range.Upper)
                {
                    break;
                }
            }

            // Load
            if (loadList.Count > 0)
            {
                goto Load;
            }

            // Read
            var dataPosition = 0;
            for (var book = range.Lower; book != null; book = book.PositionLink.Next)
            {
                if (!book.TryReadBufferInternal(start, data.Span.Slice(dataPosition), out var readLength))
                {// Fatal
                    return false;
                }

                dataPosition += readLength;
                start += (ulong)readLength;

                if (book == range.Upper)
                {// Complete
                    return true;
                }
            }

            return false;
        }
    }

    internal async Task SaveJournalAsync(bool merge)
    {
        lock (this.syncBooks)
        {
            // Flush record buffer
            this.FlushRecordBufferInternal();

            // Save all books
            Book? book = this.books.PositionChain.Last;
            Book? next = null;
            while (book != null && !book.IsSaved)
            {
                next = book;
                book = book.PositionLink.Previous;
            }

            book = next;
            while (book != null)
            {
                book.SaveInternal();
                book = book.PositionLink.Next;
            }

            // Limit memory usage
            while (this.memoryUsage > this.SimpleJournalConfiguration.MaxMemoryCapacity)
            {
                if (this.books.InMemoryChain.First is { } b)
                {
                    b.Goshujin = null;
                }
            }

            if (!merge)
            {
                return;
            }

            if (this.books.UnfinishedChain.Count >= MergeThresholdNumber ||
            this.unfinishedSize >= (ulong)this.SimpleJournalConfiguration.FinishedBookLength)
            {
                merge = true;
            }
            else
            {
                merge = false;
            }
        }

        if (merge)
        { // Merge books
            await this.Merge(false).ConfigureAwait(false);
        }
    }

    internal async Task Merge(bool forceMerge)
    {
        var book = this.books.UnfinishedChain.Last;
        var unfinishedCount = 0;
        var unfinishedLength = 0;
        var lastLength = 0;
        ulong start, end;

        lock (this.syncBooks)
        {
            while (book != null)
            {
                unfinishedCount++;
                unfinishedLength += book.Length;
                if (unfinishedLength <= this.SimpleJournalConfiguration.FinishedBookLength)
                {
                    lastLength = unfinishedLength;
                }

                book = book.UnfinishedLink.Previous;
            }

            Debug.Assert(unfinishedCount == this.books.UnfinishedChain.Count);

            if (!forceMerge)
            {
                if (unfinishedCount < MergeThresholdNumber ||
                    unfinishedLength < this.SimpleJournalConfiguration.FinishedBookLength)
                {
                    return;
                }
            }

            start = this.books.UnfinishedChain.First!.Position;
            end = start + (ulong)lastLength;
        }

        if (unfinishedCount < 2)
        {
            return;
        }

        var owner = ByteArrayPool.Default.Rent(lastLength);
        if (!await this.ReadJournalAsync(start, end, owner.ByteArray.AsMemory(0, lastLength)).ConfigureAwait(false))
        {
            owner.Return();
            return;
        }

        if (await Book.MergeBooks(this, start, end, owner.ToReadOnlyMemoryOwner(0, lastLength)).ConfigureAwait(false))
        {// Success
            this.logger.TryGet()?.Log($"Merged: {start} - {end}");
        }
    }

    private void FlushRecordBufferInternal()
    {// lock (this.syncRecordBuffer)
        if (this.recordBufferLength == 0)
        {// Empty
            return;
        }

        Book.AppendNewBook(this, this.recordBufferPosition, this.recordBuffer, this.recordBufferLength);

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

            foreach (var x in this.books)
            {
                if (x.IsUnfinished)
                {
                    this.books.UnfinishedChain.AddLast(x);
                }
            }

            this.CheckBooksInternal();
        }
    }

    private void CheckBooksInternal()
    {// lock (this.syncObject)
        ulong previousPosition = 0;
        Book? previous = null;
        Book? toDelete = null;

        foreach (var book in this.books)
        {
            if (previous != null && book.Position != previousPosition)
            {
                toDelete = previous;
            }

            previousPosition = book.NextPosition;
            previous = book;
        }

        if (toDelete == null)
        {// Ok
            if (this.books.PositionChain.Last is not null)
            {
                this.recordBufferPosition = this.books.PositionChain.Last.NextPosition;
            }
            else
            {// Initial position
                this.recordBufferPosition = 1;
            }

            return;
        }
        else
        {
            var nextBook = toDelete.PositionLink.Next;
            if (nextBook is not null)
            {
                this.recordBufferPosition = nextBook.NextPosition;
            }
            else
            {
                this.recordBufferPosition = toDelete.NextPosition;
            }
        }

        while (true)
        {// Delete books that have lost journal consistency.
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
