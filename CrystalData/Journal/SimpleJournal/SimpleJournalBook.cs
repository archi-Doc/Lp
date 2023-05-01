// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

        internal bool IsSaved => this.path != null;

        internal bool IsUnfinished => this.bookType == BookType.Unfinished;

        private SimpleJournal simpleJournal;

        [Link(Primary = true, Type = ChainType.Ordered, NoValue = true)]
        private ulong position;

        private int length;
        private BookType bookType;
        private string? path;
        private ulong hash;
        private byte[]? buffer;

        #endregion

        public static Book? TryAdd(SimpleJournal simpleJournal, PathInformation pathInformation)
        {
            BookType bookType = BookType.Unfinished;

            // BookTitle.finished or BookTitle.unfinished
            var fileName = Path.GetFileName(pathInformation.Path);
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

            book.buffer = ArrayPool<byte>.Shared.Rent(dataLength);
            data.AsSpan(0, dataLength).CopyTo(book.buffer);
            book.hash = FarmHash.Hash64(book.buffer.AsSpan(0, dataLength));

            lock (simpleJournal.syncBooks)
            {
                book.Goshujin = simpleJournal.books;
            }

            return book;
        }

        public static async Task MergeBooks(SimpleJournal simpleJournal, ulong start, ulong end, byte[] data, int dataLength)
        {
            var book = new Book(simpleJournal);
            book.position = start;
            book.length = (int)(end - start);
            book.bookType = BookType.Finished;

            book.buffer = data;
            book.hash = FarmHash.Hash64(data.AsSpan(0, dataLength));

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
            else if (this.buffer == null)
            {
                return;
            }
            else if (this.simpleJournal.rawFiler == null)
            {
                return;
            }

            // BookTitle
            var bookTitle = new BookTitle(this.position, this.hash);
            var name = bookTitle.ToBase64Url() + (this.bookType == BookType.Finished ? FinishedSuffix : UnfinishedSuffix);

            // Write (IsSaved -> true)
            this.path = PathHelper.CombineWithSlash(this.simpleJournal.SimpleJournalConfiguration.DirectoryConfiguration.Path, name);
            var owner = new ByteArrayPool.Owner(this.buffer);
            this.simpleJournal.rawFiler.WriteAndForget(this.path, 0, owner.ToReadOnlyMemoryOwner(0, this.length));
        }

        public async Task<bool> SaveAsync()
        {
            if (this.IsSaved)
            {
                return false;
            }
            else if (this.buffer == null)
            {
                return false;
            }
            else if (this.simpleJournal.rawFiler == null)
            {
                return false;
            }

            // BookTitle
            var bookTitle = new BookTitle(this.position, this.hash);
            var name = bookTitle.ToBase64Url() + (this.bookType == BookType.Finished ? FinishedSuffix : UnfinishedSuffix);

            // Write (IsSaved -> true)
            this.path = PathHelper.CombineWithSlash(this.simpleJournal.SimpleJournalConfiguration.DirectoryConfiguration.Path, name);
            var owner = new ByteArrayPool.Owner(this.buffer);
            var result = await this.simpleJournal.rawFiler.WriteAsync(this.path, 0, owner.ToReadOnlyMemoryOwner(0, this.length)).ConfigureAwait(false);

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
            => this.buffer != null;

        protected void InMemoryLinkAdded()
        {
            if (this.buffer is not null)
            {
                this.simpleJournal.memoryUsage += (ulong)this.length;
            }
        }

        protected void InMemoryLinkRemoved()
        {
            if (this.buffer is not null)
            {
                this.simpleJournal.memoryUsage -= (ulong)this.length;
                ArrayPool<byte>.Shared.Return(this.buffer);
                this.buffer = null;
            }
        }
    }
}
