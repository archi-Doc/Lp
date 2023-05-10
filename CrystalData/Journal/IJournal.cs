// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData;

public interface IJournal
{
    Task<CrystalResult> Prepare(PrepareParam param);

    void GetWriter(JournalType recordType, uint plane, out TinyhandWriter writer);

    ulong Add(in TinyhandWriter writer);

    Task SaveJournalAsync();

    Task TerminateAsync();

    ulong GetCurrentPosition();

    void ResetJournal(ulong position);
}
