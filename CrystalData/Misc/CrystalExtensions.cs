// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData;
using Tinyhand.IO;

public static class CrystalExtensions
{// -> implicit extension...
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetJournalWriter(this ITinyhandJournal journal, out TinyhandWriter writer)
    {
        if (journal.Crystal is not null)
        {
            return journal.Crystal.TryGetJournalWriter(JournalType.Record, journal.CurrentPlane, out writer);
        }
        else
        {
            writer = default;
            return false;
        }
    }

    public static bool IsSuccess(this CrystalResult result)
        => result == CrystalResult.Success;

    public static bool IsFailure(this CrystalResult result)
        => result != CrystalResult.Success;
}
