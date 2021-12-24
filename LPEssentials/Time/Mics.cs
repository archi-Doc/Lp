// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP;

/// <summary>
/// <see cref="Mics"/> represents time in microseconds (<see cref="long"/>).
/// </summary>
public static class Mics
{
    public const long MicsPerDay = 86_400_000_000;
    public const long MicsPerHour = 3_600_000_000;
    public const long MicsPerMinute = 60_000_000;
    public const long MicsPerSecond = 1_000_000;
    public const long MicsPerMillisecond = 1_000;
    public const double MicsPerNanosecond = 0.001d;
    public static readonly double TimestampToMics;

    static Mics()
    {
        TimestampToMics = 1_000_000d / Stopwatch.Frequency;
        TimeCorrection.Start();
    }

    /// <summary>
    /// Gets the number of Nsec since system startup (Stopwatch.GetTimestamp()).
    /// </summary>
    /// <returns>Nsec (Nanosecond).</returns>
    public static long GetSystem() => (long)(Stopwatch.GetTimestamp() * TimestampToMics);

    /// <summary>
    /// Gets the number of ticks since LP has started.<br/>
    /// Not affected by manual date/time changes.
    /// </summary>
    /// <returns>Ticks.</returns>
    public static long GetApplication() => (long)(Stopwatch.GetTimestamp() * TimestampToMics) - TimeCorrection.InitialSystemMics;

    /// <summary>
    /// Gets the number of ticks expressed as UTC.
    /// </summary>
    /// <returns>Ticks.</returns>
    public static long GetUtcNow() => (long)(DateTime.UtcNow.Ticks * TimestampToMics);

    /// <summary>
    /// Get the corrected <see cref="Mics"/> expressed as UTC.
    /// </summary>
    /// <param name="correctedMics">The corrected <see cref="Mics"/>.</param>
    /// <returns><see cref="CorrectedResult"/>.</returns>
    public static CorrectedResult GetCorrected(out long correctedMics) => TimeCorrection.GetCorrectedMics(out correctedMics);

    public static long FromDays(double days) => (long)(days * MicsPerDay);

    public static long FromHours(double hours) => (long)(hours * MicsPerHour);

    public static long FromMinutes(double minutes) => (long)(minutes * MicsPerMinute);

    public static long FromSeconds(double seconds) => (long)(seconds * MicsPerSecond);

    public static long FromMilliseconds(double milliseconds) => (long)(milliseconds * MicsPerMillisecond);

    public static long FromMicroseconds(double microseconds) => (long)microseconds;

    public static long FromNanoseconds(double nanoseconds) => (long)(nanoseconds * MicsPerNanosecond);
}
