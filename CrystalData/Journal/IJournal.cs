// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Journal;
using Tinyhand.IO;

namespace CrystalData;

public interface IJournal
{
    Task<CrystalResult> Prepare(PrepareParam param);

    bool Prepared { get; }

    uint NewToken(IJournalObject journalObject);

    bool RegisterToken(uint token, IJournalObject journalObject);

    uint UpdateToken(uint oldToken, IJournalObject journalObject);

    bool UnregisterToken(uint token);

    void GetWriter(JournalRecordType recordType, uint token, out TinyhandWriter writer);

    ulong Add(in TinyhandWriter writer);
}
