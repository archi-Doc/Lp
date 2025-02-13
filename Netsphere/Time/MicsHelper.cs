// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public static class MicsHelper
{
    public static string ToString(long mics, string? format = null) => Mics.MicsToDateTime(mics).ToString(format);

    public static string ToSimpleString(long mics)
    {
        var ts = MicsToTimeSpan(mics);
        return ts.TotalDays >= 1
            ? $"{ts.TotalDays:0.00}d"
            : ts.TotalHours >= 1
                ? $"{ts.TotalHours:0.00}h"
                : ts.TotalMinutes >= 1
                    ? $"{ts.TotalMinutes:0.00}m"
                    : ts.TotalSeconds >= 1
                        ? $"{ts.TotalSeconds:0.00}s"
                        : ts.TotalMilliseconds >= 1
                            ? $"{ts.TotalMilliseconds:0.00}ms"
                            : $"{mics}μs";
    }
}
