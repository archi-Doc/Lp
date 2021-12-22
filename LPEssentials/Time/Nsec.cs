// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP;

public static class Nsec
{
    public const long NsecPerDay = 86_400_000_000_000;
    public const long NsecPerHour = 3_600_000_000_000;
    public const long NsecPerMinute = 60_000_000_000;
    public const long NsecPerSecond = 1_000_000_000;
    public const long NsecPerMillisecond = 1_000_000;
    public const long NsecPerMicrosecond = 1_000;
    public static readonly long TimestampToNsec;

    static Nsec()
    {
        if (Stopwatch.Frequency < 1_000_000_000)
        {
            TimestampToNsec = 1_000_000_000 / Stopwatch.Frequency;
        }
        else
        {
            TimestampToNsec = 1;
        }
    }

    /// <summary>
    /// Gets the number of Nsec since system startup (Stopwatch.GetTimestamp()).
    /// </summary>
    /// <returns>Nsec (Nanosecond).</returns>
    public static long GetSystem() => Stopwatch.GetTimestamp() * TimestampToNsec;

    /// <summary>
    /// Gets the number of ticks since LP has started.<br/>
    /// Not affected by manual date/time changes.
    /// </summary>
    /// <returns>Ticks.</returns>
    public static long GetApplication() => (Stopwatch.GetTimestamp() * TimestampToNsec) - TimeCorrection.InitialSystemNsec;

    /// <summary>
    /// Gets the number of ticks expressed as UTC.
    /// </summary>
    /// <returns>Ticks.</returns>
    public static long GetUtcNow() => DateTime.UtcNow.Ticks * TimestampToNsec;

    /// <summary>
    /// Get the number of corrected ticks expressed as UTC.
    /// </summary>
    /// <param name="correctedTicks">The corrected ticks.</param>
    /// <returns><see cref="CorrectedResult"/>.</returns>
    public static CorrectedResult GetCorrected(out long correctedTicks) => TimeCorrection.GetCorrectedNsec(out correctedTicks);

    public static long FromDays(double days) => (long)(days * NsecPerDay);

    public static long FromHours(double hours) => (long)(hours * NsecPerHour);

    public static long FromMinutes(double minutes) => (long)(minutes * NsecPerMinute);

    public static long FromSeconds(double seconds) => (long)(seconds * NsecPerSecond);

    public static long FromMilliseconds(double milliseconds) => (long)(milliseconds * NsecPerMillisecond);

    public static long FromMicroseconds(double microseconds) => (long)(microseconds * NsecPerMicrosecond);

    public static long FromNanoseconds(double nanoseconds) => (long)nanoseconds;
}
