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
        [Link(Type = ChainType.LinkedList, Name = "InMemory", AutoLink = false)]
        private Book(SimpleJournal simpleJournal)
        {
            this.simpleJournal = simpleJournal;
        }

        public static Book? TryAdd(SimpleJournal simpleJournal, PathInformation pathInformation)
        {
            BookType bookType = BookType.Unfinished;

            // File Base64.book
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

            var bytes = Base64.Url.FromStringToByteArray(fileName);
            if (bytes.Length != PositionLength)
            {
                return null;
            }

            var position = BitConverter.ToUInt64(bytes);
            var book = new Book(simpleJournal);
            book.position = position;
            book.length = (ulong)pathInformation.Length;
            book.path = pathInformation.Path;
            book.bookType = bookType;
            book.Goshujin = simpleJournal.books;

            return book;
        }

        public static Book AppendNewBook(SimpleJournal simpleJournal, Book? previousBook)
        {
            var book = new Book(simpleJournal);

            if (previousBook == null)
            {
                book.position = 1;
            }
            else
            {
                book.position = previousBook.position + previousBook.length;
            }

            book.buffer = ArrayPool<byte>.Shared.Rent(SimpleJournal.FinishedBookLength);

            simpleJournal.books.Add(book);
            simpleJournal.books.InMemoryChain.AddLast(book);

            return book;
        }

        public ulong AppendData(ReadOnlySpan<byte> data)
        {
            if (this.buffer == null || (this.buffer.Length - this.bufferPosition) < data.Length)
            {
                throw new InvalidOperationException();
            }

            var position = this.position + (ulong)this.bufferPosition;
            data.CopyTo(this.buffer.AsSpan(this.bufferPosition));
            this.bufferPosition += data.Length;
            this.length += (ulong)data.Length;
            return position;
        }

        public bool EnsureBuffer(int length)
        {
            if (this.buffer == null)
            {
            }

            return true;
        }

        public void SaveInternal()
        {
            if (this.IsSaved)
            {
                return;
            }
            else if (this.bufferPosition == 0 || this.buffer == null)
            {
                return;
            }
            else if (this.simpleJournal.rawFiler == null)
            {
                return;
            }

            // Fix buffer
            var newBuffer = ArrayPool<byte>.Shared.Rent(this.bufferPosition);
            this.buffer.AsSpan(0, this.bufferPosition).CopyTo(newBuffer);
            this.bufferPosition = 0;
            ArrayPool<byte>.Shared.Return(this.buffer);

            this.buffer = newBuffer;

            // Write
            Span<byte> span = stackalloc byte[PositionLength];
            BitConverter.TryWriteBytes(span, this.position);
            this.path = PathHelper.CombineWithSlash(this.simpleJournal.SimpleJournalConfiguration.DirectoryConfiguration.Path, Base64.Url.FromByteArrayToString(span));
            this.simpleJournal.rawFiler.WriteAndForget(this.path, 0, new(this.buffer));
        }

        public void DeleteInternal()
        {
            if (this.simpleJournal.rawFiler is { } rawFiler && this.path != null)
            {
                this.simpleJournal.rawFiler.DeleteAndForget(this.path);
            }

            if (this.buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(this.buffer);
            }

            this.Goshujin = null;
        }

        #region PropertyAndField

        internal ulong Length => this.length;

        internal bool IsSaved => this.path != null;

        private SimpleJournal simpleJournal;

        [Link(Primary = true, Type = ChainType.Ordered)]
        private ulong position;

        private ulong length;
        private BookType bookType;
        private string? path;
        private byte[]? buffer;
        private int bufferPosition;

        #endregion
    }
}
