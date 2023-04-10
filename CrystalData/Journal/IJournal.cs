// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Journal;
using Tinyhand.IO;

namespace CrystalData;

public interface IJournal
{
    Task<CrystalStartResult> Prepare();

    bool Prepared { get; }

    void GetJournalWriter(JournalRecordType recordType, out TinyhandWriter writer);

    ulong AddRecord(in TinyhandWriter writer);
}

internal interface IJournalInternal : IJournal
{
}
