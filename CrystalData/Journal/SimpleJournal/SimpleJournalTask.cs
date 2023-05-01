// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
            while (await core.Delay(1_000).ConfigureAwait(false))
            {
                // Flush record buffer
                core.simpleJournal.FlushRecordBuffer();

                await core.simpleJournal.SaveBooksAsync(true);
            }

            // Flush record buffer
            core.simpleJournal.FlushRecordBuffer();

            await core.simpleJournal.SaveBooksAsync(false);

            // Terminate
            core.simpleJournal.logger.TryGet()?.Log($"SimpleJournal terminated - {core.simpleJournal.memoryUsage}");
        }

        private SimpleJournal simpleJournal;
    }
}
