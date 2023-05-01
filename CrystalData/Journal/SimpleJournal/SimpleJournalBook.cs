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

        /*public ulong AppendData(ReadOnlySpan<byte> data)
        {
            if (this.buffer == null || (this.buffer.Length - this.bufferPosition) < data.Length)
            {
                throw new InvalidOperationException();
            }

            var position = this.position + (ulong)this.bufferPosition;
            data.CopyTo(this.buffer.AsSpan(this.bufferPosition));
            this.bufferPosition += data.Length;
            this.length += data.Length;
            return position;
        }*/

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

            // Fix buffer
            /*var newBuffer = ArrayPool<byte>.Shared.Rent(this.bufferPosition);
            this.buffer.AsSpan(0, this.bufferPosition).CopyTo(newBuffer);
            this.bufferPosition = 0;
            ArrayPool<byte>.Shared.Return(this.buffer);

            this.buffer = newBuffer;*/

            // BookTitle
            var bookTitle = new BookTitle(this.position, this.hash);
            var name = bookTitle.ToBase64Url() + (this.bookType == BookType.Finished ? FinishedSuffix : UnfinishedSuffix);

            // Write (IsSaved -> true)
            this.path = PathHelper.CombineWithSlash(this.simpleJournal.SimpleJournalConfiguration.DirectoryConfiguration.Path, name);
            this.simpleJournal.rawFiler.WriteAndForget(this.path, 0, new(this.length));
        }

        public void FreeInternal()
        {
            this.Goshujin = null;
        }

        public void DeleteInternal()
        {
            if (this.simpleJournal.rawFiler is { } rawFiler && this.path != null)
            {
                this.simpleJournal.rawFiler.DeleteAndForget(this.path);
            }

            this.FreeInternal();
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
