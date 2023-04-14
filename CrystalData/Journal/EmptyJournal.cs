// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData.Journal;

public class EmptyJournal : IJournalInternal
{
    Task<CrystalStartResult> IJournal.Prepare(Crystalizer crystalizer)
    {
        this.Prepared = true;
        return Task.FromResult(CrystalStartResult.Success);
    }

    ulong IJournal.Add(in TinyhandWriter writer)
    {
        return 0;
    }

    void IJournal.GetWriter(JournalRecordType recordType, uint token, out TinyhandWriter writer)
    {
        writer = default(TinyhandWriter);
    }

    void IJournal.UpdateToken(ref Waypoint waypoint)
    {
    }

    public bool Prepared { get; private set; }
}
