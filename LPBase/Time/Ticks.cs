// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LP;

public struct Ticks
{
    public const long TicksPerDay = TimeSpan.TicksPerDay;
    public const long TicksPerHour = TimeSpan.TicksPerHour;
    public const long TicksPerMinute = TimeSpan.TicksPerMinute;
    public const long TicksPerSecond = TimeSpan.TicksPerSecond;
    public const long TicksPerMillisecond = TimeSpan.TicksPerMillisecond;
    public const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
    public const double TicksPerNanosecond = TimeSpan.TicksPerMillisecond / 1000000d;
    // public const long NanosecondsPerTicks = 1_000_000 / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// Stopwatch.GetTimestamp().
    /// </summary>
    /// <returns>A long integer representing the tick counter value of the underlying timer mechanism.</returns>
    public static long GetTimestamp() => Stopwatch.GetTimestamp();

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
        this.LowerBound = Ticks.GetTimestamp();
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
