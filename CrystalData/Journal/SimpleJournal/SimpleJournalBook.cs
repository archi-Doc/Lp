// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using CrystalData.Filer;

namespace CrystalData.Journal;

public partial class SimpleJournal
{
    private enum BookType
    {
        Unfinished,
        Finished,
    }

    [ValueLinkObject]
    private partial class Book
    {
        [Link(Type = ChainType.LinkedList, Name = "InMemory")]
        [Link(Type = ChainType.LinkedList, Name = "Unfinished")]
        private Book(SimpleJournal simpleJournal)
        {
            this.simpleJournal = simpleJournal;
        }

        #region PropertyAndField

        internal ulong Position => this.position;

        internal ulong NextPosition => this.position + (ulong)this.length;

        internal int Length => this.length;

        internal string? Path => this.path;

        internal bool IsSaved => this.path != null;

        internal bool IsInMemory => this.memoryOwner.Memory.Length > 0;

        internal bool IsUnfinished => this.bookType == BookType.Unfinished;

        private SimpleJournal simpleJournal;

        [Link(Primary = true, Type = ChainType.Ordered, NoValue = true)]
        private ulong position;

        private int length;
        private BookType bookType;
        private string? path;
        private ulong hash;
        private ByteArrayPool.ReadOnlyMemoryOwner memoryOwner;

        #endregion

        public static Book? TryAdd(SimpleJournal simpleJournal, PathInformation pathInformation)
        {
            BookType bookType = BookType.Unfinished;

            // BookTitle.finished or BookTitle.unfinished
            var fileName = System.IO.Path.GetFileName(pathInformation.Path);
            if (fileName.EndsWith(FinishedSuffix))
            {
                bookType = BookType.Finished;
                fileName = fileName.Substring(0, fileName.Length - FinishedSuffix.Length);
            }
            else if (fileName.EndsWith(UnfinishedSuffix))
            {
                bookType = BookType.Unfinished;
                fileName = fileName.Substring(0, fileName.Length - UnfinishedSuffix.Length);
            }
            else
            {
                return null;
            }

            if (!BookTitle.TryParse(fileName, out var bookTitle))
            {
                return null;
            }

            var book = new Book(simpleJournal);
            book.position = bookTitle.JournalPosition;
            book.length = (int)pathInformation.Length;
            book.path = pathInformation.Path;
            book.hash = bookTitle.Hash;
            book.bookType = bookType;

            book.Goshujin = simpleJournal.books;

            return book;
        }

        public static Book AppendNewBook(SimpleJournal simpleJournal, ulong position, byte[] data, int dataLength)
        {
            var book = new Book(simpleJournal);
            book.position = position;
            book.length = dataLength;
            book.bookType = BookType.Unfinished;

            var owner = ByteArrayPool.Default.Rent(dataLength);
            data.AsSpan(0, dataLength).CopyTo(owner.ByteArray.AsSpan());
            book.memoryOwner = owner.ToReadOnlyMemoryOwner(0, dataLength);
            book.hash = FarmHash.Hash64(book.memoryOwner.Memory.Span);

            lock (simpleJournal.syncBooks)
            {
                book.Goshujin = simpleJournal.books;
            }

            return book;
        }

        public static async Task MergeBooks(SimpleJournal simpleJournal, ulong start, ulong end, ByteArrayPool.ReadOnlyMemoryOwner toBeMoved)
        {
            var book = new Book(simpleJournal);
            book.position = start;
            book.length = (int)(end - start);
            book.bookType = BookType.Finished;

            book.memoryOwner = toBeMoved;
            book.hash = FarmHash.Hash64(toBeMoved.Memory.Span);

            // Save the book first
            await book.SaveAsync().ConfigureAwait(false);

            lock (simpleJournal.syncBooks)
            {
                var range = simpleJournal.books.PositionChain.GetRange(start, end - 1);
                if (range.Lower == null || range.Upper == null)
                {
                    return;
                }
                else if (range.Lower.position != start || range.Upper.NextPosition != end)
                {
                    return;
                }

                // Delete books
                var b = range.Lower;
                while (b != null)
                {
                    var b2 = b.PositionLink.Next;
                    b.DeleteInternal();
                    if (b == range.Upper)
                    {
                        break;
                    }

                    b = b2;
                }

                book.Goshujin = simpleJournal.books;
            }
        }

        public void SaveInternal()
        {// lock (core.simpleJournal.syncBooks)
            if (this.IsSaved)
            {
                return;
            }
            else if (!this.IsInMemory)
            {
                return;
            }
            else if (this.simpleJournal.rawFiler == null)
            {
                return;
            }

            // Write (IsSaved -> true)
            this.path = PathHelper.CombineWithSlash(this.simpleJournal.SimpleJournalConfiguration.DirectoryConfiguration.Path, this.GetFileName());
            this.simpleJournal.rawFiler.WriteAndForget(this.path, 0, this.memoryOwner);
        }

        public bool TryReadBufferInternal(ulong position, Span<byte> destination, out int readLength)
        {
            readLength = 0;
            if (position < this.position || position >= this.NextPosition)
            {
                return false;
            }

            var length = (int)(this.NextPosition - position);
            if (destination.Length < length)
            {
                return false;
            }

            if (!this.IsInMemory)
            {
                return false;
            }

            this.memoryOwner.Memory.Span.Slice((int)(position - this.position), length).CopyTo(destination);
            readLength = length;
            return true;
        }

        public async Task<bool> SaveAsync()
        {
            if (this.IsSaved)
            {
                return false;
            }
            else if (!this.IsInMemory)
            {
                return false;
            }
            else if (this.simpleJournal.rawFiler == null)
            {
                return false;
            }

            // Write (IsSaved -> true)
            this.path = PathHelper.CombineWithSlash(this.simpleJournal.SimpleJournalConfiguration.DirectoryConfiguration.Path, this.GetFileName());
            var owner = this.memoryOwner.IncrementAndShare();
            var result = await this.simpleJournal.rawFiler.WriteAsync(this.path, 0, owner).ConfigureAwait(false);

            return result.IsSuccess();
        }

        public void DeleteInternal()
        {
            if (this.simpleJournal.rawFiler is { } rawFiler && this.path != null)
            {
                this.simpleJournal.rawFiler.DeleteAndForget(this.path);
            }

            this.Goshujin = null;
        }

        public bool TrySetBuffer(ByteArrayPool.ReadOnlyMemoryOwner data)
        {
            if (this.IsInMemory)
            {
                return false;
            }
            else if (this.length != data.Memory.Length)
            {
                return false;
            }

            this.memoryOwner = data.IncrementAndShare();
            this.simpleJournal.books.InMemoryChain.AddLast(this);
            this.InMemoryLinkAdded();

            return true;
        }

        protected bool UnfinishedLinkPredicate()
            => this.IsUnfinished;

        protected void UnfinishedLinkAdded()
        {
            this.simpleJournal.unfinishedSize += (ulong)this.length;
        }

        protected void UnfinishedLinkRemoved()
        {
            this.simpleJournal.unfinishedSize -= (ulong)this.length;
        }

        protected bool InMemoryLinkPredicate()
            => this.IsInMemory;

        protected void InMemoryLinkAdded()
        {
            this.simpleJournal.memoryUsage += this.memoryOwner.Memory.Length;
        }

        protected void InMemoryLinkRemoved()
        {
            this.simpleJournal.memoryUsage -= this.memoryOwner.Memory.Length;
            this.memoryOwner = this.memoryOwner.Return();
        }

        private string GetFileName()
        {
            var bookTitle = new BookTitle(this.position, this.hash);
            return bookTitle.ToBase64Url() + (this.bookType == BookType.Finished ? FinishedSuffix : UnfinishedSuffix);
        }
    }
}
