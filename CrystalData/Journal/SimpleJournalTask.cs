// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using CrystalData.Filer;

namespace CrystalData.Journal;

public partial class SimpleJournal
{
    private class SimpleJournalTask : TaskCore
    {
        public SimpleJournalTask(SimpleJournal simpleJournal)
            : base(null, Process)
        {
            this.simpleJournal = simpleJournal;
        }

        private static async Task Process(object? parameter)
        {
            var core = (SimpleJournalTask)parameter!;
            while (await core.Delay(1000).ConfigureAwait(false))
            {
                lock (core.simpleJournal.syncObject)
                {
                    Book? book = core.simpleJournal.books.PositionChain.First;
                    while (book != null && !book.IsSaved)
                    {
                        book = book.PositionLink.Previous;
                    }

                    while (book != null)
                    {
                        book.SaveInternal();
                        book = book.PositionLink.Next;
                    }
                }
            }
        }

        private SimpleJournal simpleJournal;
    }
}
