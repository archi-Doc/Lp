// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP;

public static class Ticks
{
    public const long TicksPerDay = TimeSpan.TicksPerDay;
    public const long TicksPerHour = TimeSpan.TicksPerHour;
    public const long TicksPerMinute = TimeSpan.TicksPerMinute;
    public const long TicksPerSecond = TimeSpan.TicksPerSecond;
    public const long TicksPerMillisecond = TimeSpan.TicksPerMillisecond;
    public const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
    public const double TicksPerNanosecond = TimeSpan.TicksPerMillisecond / 1000000d;

    /// <summary>
    /// Gets the number of ticks since system startup (Stopwatch.GetTimestamp()).
    /// </summary>
    /// <returns>Ticks.</returns>
    public static long GetSystem() => Stopwatch.GetTimestamp();

    /// <summary>
    /// Gets the number of ticks since LP has started.<br/>
    /// Not affected by manual date/time changes.
    /// </summary>
    /// <returns>Ticks.</returns>
    public static long GetApplication() => Stopwatch.GetTimestamp() - TimeCorrection.InitialSystemTicks;

    /// <summary>
    /// Gets the number of ticks expressed as UTC.
    /// </summary>
    /// <returns>Ticks.</returns>
    public static long GetUtcNow() => DateTime.UtcNow.Ticks;

    /// <summary>
    /// Get the number of corrected ticks expressed as UTC.
    /// </summary>
    /// <param name="correctedTicks">The corrected ticks.</param>
    /// <returns><see cref="CorrectedResult"/>.</returns>
    public static CorrectedResult GetCorrected(out long correctedTicks) => TimeCorrection.GetCorrectedTicks(out correctedTicks);

    public static long FromDays(double days) => (long)(days * TicksPerDay);

    public static long FromHours(double hours) => (long)(hours * TicksPerHour);

    public static long FromMinutes(double minutes) => (long)(minutes * TicksPerMinute);

    public static long FromSeconds(double seconds) => (long)(seconds * TicksPerSecond);

    public static long FromMilliseconds(double milliseconds) => (long)(milliseconds * TicksPerMillisecond);

    public static long FromMicroseconds(double microseconds) => (long)(microseconds * TicksPerMicrosecond);

    public static long FromNanoseconds(double nanoseconds) => (long)(nanoseconds * TicksPerNanosecond);
}
