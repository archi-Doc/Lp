// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LP;

public static class Time
{
    public static readonly double TimestampToTicks;
    public static readonly double MicsToTicks;
    private static readonly long FixedTimestamp; // Fixed timestamp at application startup.
    private static readonly DateTime FixedUtcNow; // Fixed DateTime at application startup.

    static Time()
    {
        TimestampToTicks = 10_000_000d / Stopwatch.Frequency;
        MicsToTicks = TimestampToTicks / Mics.TimestampToMics;
        FixedTimestamp = Stopwatch.GetTimestamp();
        FixedUtcNow = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a DateTime since system startup (Stopwatch.GetTimestamp()).
    /// </summary>
    /// <returns><see cref="DateTime"/>.</returns>
    public static DateTime GetSystem() => new DateTime((long)(Stopwatch.GetTimestamp() * TimestampToTicks));

    /// <summary>
    /// Gets a <see cref="DateTime"/> since LP has started (0001/01/01 0:00:00).<br/>
    /// Not affected by manual date/time changes.
    /// </summary>
    /// <returns><see cref="DateTime"/>.</returns>
    public static DateTime GetApplication() => new DateTime((long)(Mics.GetApplication() * MicsToTicks));

    /// <summary>
    /// Gets a <see cref="DateTime"/> expressed as UTC.
    /// </summary>
    /// <returns><see cref="DateTime"/>.</returns>
    public static DateTime GetUtcNow() => DateTime.UtcNow;

    /// <summary>
    /// Gets a fixed <see cref="DateTime"/> expressed as UTC.
    /// Not affected by manual date/time changes.
    /// </summary>
    /// <returns><see cref="DateTime"/>.</returns>
    public static DateTime GetFixedUtcNow()
        => FixedUtcNow + TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - FixedTimestamp) * TimestampToTicks));

    /// <summary>
    /// Get a corrected <see cref="DateTime"/> expressed as UTC.
    /// </summary>
    /// <param name="correctedTime">The corrected time.</param>
    /// <returns><see cref="CorrectedResult"/>.</returns>
    public static CorrectedResult GetCorrected(out DateTime correctedTime)
    {
        var result = TimeCorrection.GetCorrectedMics(out var mics);
        correctedTime = new DateTime((long)(mics * MicsToTicks));
        return result;
    }
}
