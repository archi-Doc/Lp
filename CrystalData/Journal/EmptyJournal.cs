// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData.Journal;

public class EmptyJournal : IJournal
{
    Task<CrystalResult> IJournal.Prepare(PrepareParam param)
    {
        return Task.FromResult(CrystalResult.Success);
    }

    ulong IJournal.Add(in TinyhandWriter writer)
    {
        return 0;
    }

    void IJournal.GetWriter(JournalRecordType recordType, uint token, out TinyhandWriter writer)
    {
        writer = default(TinyhandWriter);
    }

    uint IJournal.NewToken(IJournalObject journalObject)
    {
        return 1;
    }

    bool IJournal.RegisterToken(uint token, IJournalObject journalObject)
    {
        return true;
    }

    uint IJournal.UpdateToken(uint oldToken, IJournalObject journalObject)
    {
        return 1;
    }

    bool IJournal.UnregisterToken(uint token)
    {
        return true;
    }
}
