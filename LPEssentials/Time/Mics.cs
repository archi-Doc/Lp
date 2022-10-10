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
    private static readonly long FixedMics; // Fixed mics at application startup.

    static Mics()
    {
        TimestampToMics = 1_000_000d / Stopwatch.Frequency;
        FixedMics = GetUtcNow() - (long)(Stopwatch.GetTimestamp() * TimestampToMics);

        TimeCorrection.Start();
    }

    /// <summary>
    /// Gets the <see cref="Mics"/> (microseconds) since system startup (Stopwatch.GetTimestamp()).
    /// </summary>
    /// <returns><see cref="Mics"/> (microseconds).</returns>
    public static long GetSystem() => (long)(Stopwatch.GetTimestamp() * TimestampToMics);

    /// <summary>
    /// Gets the <see cref="Mics"/> (microseconds) since LP has started.<br/>
    /// Not affected by manual date/time changes.
    /// </summary>
    /// <returns><see cref="Mics"/> (microseconds).</returns>
    public static long GetApplication() => (long)(Stopwatch.GetTimestamp() * TimestampToMics) - TimeCorrection.InitialSystemMics;

    /// <summary>
    /// Gets the <see cref="Mics"/> (microseconds) expressed as UTC.
    /// </summary>
    /// <returns><see cref="Mics"/> (microseconds).</returns>
    public static long GetUtcNow() => (long)(DateTime.UtcNow.Ticks * 0.1d);

    /// <summary>
    /// Gets the fixed <see cref="Mics"/> (microseconds) expressed as UTC.
    /// Not affected by manual date/time changes.
    /// </summary>
    /// <returns><see cref="Mics"/> (microseconds).</returns>
    public static long GetFixedUtcNow() => FixedMics + GetSystem();

    /// <summary>
    /// Get the corrected <see cref="Mics"/> expressed as UTC.
    /// </summary>
    /// <returns>The corrected <see cref="Mics"/>.</returns>
    public static long GetCorrected()
    {
        long mics;
        if (Time.NtpCorrection is { } ntpCorrection)
        {// Ntp correction
            ntpCorrection.TryGetCorrectedMics(out mics);
            return mics;
        }

        TimeCorrection.GetCorrectedMics(out mics);
        return mics;
    }

    public static long FromDays(double days) => (long)(days * MicsPerDay);

    public static long FromHours(double hours) => (long)(hours * MicsPerHour);

    public static long FromMinutes(double minutes) => (long)(minutes * MicsPerMinute);

    public static long FromSeconds(double seconds) => (long)(seconds * MicsPerSecond);

    public static long FromMilliseconds(double milliseconds) => (long)(milliseconds * MicsPerMillisecond);

    public static long FromMicroseconds(double microseconds) => (long)microseconds;

    public static long FromNanoseconds(double nanoseconds) => (long)(nanoseconds * MicsPerNanosecond);

    public static DateTime ToDateTime(long mics) => new DateTime((long)((double)mics * Time.MicsToTicks));

    public static string ToString(long mics, string? format = null) => ToDateTime(mics).ToString(format);
}
