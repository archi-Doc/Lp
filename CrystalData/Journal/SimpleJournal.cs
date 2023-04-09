// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData.Journal;

public class SimpleJournal : IJournal
{
    public SimpleJournal(Crystalizer crystalizer, SimpleJournalConfiguration configuration)
    {
        this.crystalizer = crystalizer;
        this.SimpleJournalConfiguration = configuration;
    }

    public async Task<CrystalStartResult> Prepare()
    {
        this.rawFiler ??= this.crystalizer.ResolveRawFiler(this.SimpleJournalConfiguration.DirectoryConfiguration);

        return CrystalStartResult.Success;
    }

    public SimpleJournalConfiguration SimpleJournalConfiguration { get; }

    public bool Prepared { get; private set; }

    private Crystalizer crystalizer;
    private IRawFiler? rawFiler;
}
