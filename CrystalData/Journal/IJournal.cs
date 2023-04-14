// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Journal;
using Tinyhand.IO;

namespace CrystalData;

public interface IJournal
{
    Task<CrystalStartResult> Prepare(Crystalizer crystalizer);

    bool Prepared { get; }

    void GetWriter(JournalRecordType recordType, out TinyhandWriter writer);

    ulong Add(in TinyhandWriter writer);
}

internal interface IJournalInternal : IJournal
{
}
