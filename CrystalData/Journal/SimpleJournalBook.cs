// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;

namespace CrystalData.Journal;

public partial class SimpleJournal
{
    [ValueLinkObject]
    private partial class SimpleJournalBook
    {
        [Link(Type = ChainType.LinkedList, Name = "InMemory", AutoLink = false)]
        public SimpleJournalBook()
        {
        }

        public static SimpleJournalBook AppendNewBook(SimpleJournal journal, SimpleJournalBook? previousBook)
        {
            var book = new SimpleJournalBook();

            if (previousBook == null)
            {
                book.bookStart = 1;
            }
            else
            {
                book.bookStart = previousBook.bookStart + previousBook.bookLength;
            }

            book.buffer = ArrayPool<byte>.Shared.Rent(SimpleJournal.MaxBookLength);

            journal.books.Add(book);
            journal.books.InMemoryChain.AddLast(book);

            return book;
        }

        public ulong AppendData(ReadOnlySpan<byte> data)
        {
            if (this.buffer == null || (this.buffer.Length - this.bufferPosition) < data.Length)
            {
                throw new InvalidOperationException();
            }

            var waypoint = this.bookStart + (ulong)this.bufferPosition;
            data.CopyTo(this.buffer.AsSpan(this.bufferPosition));
            this.bufferPosition += data.Length;
            this.bookLength += (ulong)data.Length;
            return waypoint;
        }

        public bool EnsureBuffer(int length)
        {
            if (this.buffer == null)
            {
            }

            return true;
        }

        public string GetFileName()
        {
            Span<byte> buffer = stackalloc byte[8];
            BitConverter.TryWriteBytes(buffer, this.bookStart);
            var hex = Arc.Crypto.Hex.FromByteArrayToString(buffer);
            return hex + BookSuffix;
        }

        #region PropertyAndField

        [Link(Primary = true, Type = ChainType.Ordered)]
        private ulong bookStart;

        private ulong bookLength;

        private byte[]? buffer;
        private int bufferPosition;

        #endregion
    }
}
