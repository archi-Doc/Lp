// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Journal;

public partial class SimpleJournal
{
    private const int SaveIntervalInMilliseconds = 1_000;

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
            while (await core.Delay(SaveIntervalInMilliseconds).ConfigureAwait(false))
            {
                await core.simpleJournal.SaveJournalAsync(true);
            }

            await core.simpleJournal.SaveJournalAsync(false);
        }

        private SimpleJournal simpleJournal;
    }
}
