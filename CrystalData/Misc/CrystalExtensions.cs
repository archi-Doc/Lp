// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData;
using Tinyhand.IO;

public static class CrystalExtensions
{// -> implicit extension...
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetJournalWriter(this ITreeObject obj, out TinyhandWriter writer)
    {
        if (obj.Journal is not null)
        {
            return obj.Journal.TryGetJournalWriter(JournalType.Record, out writer);
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

    public static bool IsUnload(this UnloadMode unloadMode)
        => unloadMode != UnloadMode.NoUnload;
}
