// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Journal;
using Tinyhand.IO;

namespace CrystalData;

public interface IJournal
{
    Task<CrystalResult> Prepare(PrepareParam param);

    void GetWriter(JournalRecordType recordType, uint plane, out TinyhandWriter writer);

    ulong Add(in TinyhandWriter writer);

    Task TerminateAsync();
}
