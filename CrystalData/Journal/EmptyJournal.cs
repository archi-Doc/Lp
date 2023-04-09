// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Journal;

public class EmptyJournal : IJournal
{
    Task<CrystalStartResult> IJournal.Prepare()
    {
        this.Prepared = true;
        return Task.FromResult(CrystalStartResult.Success);
    }

    public bool Prepared { get; private set; }
}
