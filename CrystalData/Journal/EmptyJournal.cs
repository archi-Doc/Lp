// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;
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

    void IJournal.GetWriter(JournalType recordType, uint token, out TinyhandWriter writer)
    {
        writer = default(TinyhandWriter);
    }

    Task IJournal.SaveJournalAsync()
    {
        return Task.CompletedTask;
    }

    Task IJournal.TerminateAsync()
    {
        return Task.CompletedTask;
    }

    ulong IJournal.GetCurrentPosition() => 1;

    void IJournal.ResetJournal(ulong position)
    {
    }

    Task<(ulong NextPosition, ByteArrayPool.MemoryOwner Data)> IJournal.ReadJournalAsync(ulong position)
        => Task.FromResult((default(ulong), default(ByteArrayPool.MemoryOwner)));
}
