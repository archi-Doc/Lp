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
    public static long GetUtc() => DateTime.UtcNow.Ticks;

    /// <summary>
    /// Get the corrected <see cref="DateTime"/> expressed as UTC.
    /// </summary>
    /// <param name="correctedTime">The corrected time.</param>
    /// <returns><see cref="CorrectedResult"/>.</returns>
    public static CorrectedResult GetCorrected(out long correctedTime) => TimeCorrection.GetCorrectedTicks(out correctedTime);

    public static long FromDays(double days) => (long)(days * TicksPerDay);

    public static long FromHours(double hours) => (long)(hours * TicksPerHour);

    public static long FromMinutes(double minutes) => (long)(minutes * TicksPerMinute);

    public static long FromSeconds(double seconds) => (long)(seconds * TicksPerSecond);

    public static long FromMilliseconds(double milliseconds) => (long)(milliseconds * TicksPerMillisecond);

    public static long FromMicroseconds(double microseconds) => (long)(microseconds * TicksPerMicrosecond);

    public static long FromNanoseconds(double nanoseconds) => (long)(nanoseconds * TicksPerNanosecond);
}

public struct TicksRange
{
    public TicksRange(long ticks)
    {
        this.LowerBound = Ticks.GetSystem();
        this.UpperBound = this.LowerBound + ticks;
    }

    public static TicksRange FromDays(double days) => new TicksRange((long)(days * Ticks.TicksPerDay));

    public static TicksRange FromHours(double hours) => new TicksRange((long)(hours * Ticks.TicksPerHour));

    public static TicksRange FromMinutes(double minutes) => new TicksRange((long)(minutes * Ticks.TicksPerMinute));

    public static TicksRange FromSeconds(double seconds) => new TicksRange((long)(seconds * Ticks.TicksPerSecond));

    public static TicksRange FromMilliseconds(double milliseconds) => new TicksRange((long)(milliseconds * Ticks.TicksPerMillisecond));

    public static TicksRange FromMicroseconds(double microseconds) => new TicksRange((long)(microseconds * Ticks.TicksPerMicrosecond));

    public static TicksRange FromNanoseconds(double nanoseconds) => new TicksRange((long)(nanoseconds * Ticks.TicksPerNanosecond));

    public bool IsIn(long ticks) => this.LowerBound <= ticks && ticks <= this.UpperBound;

    public readonly long LowerBound;

    public readonly long UpperBound;
}
