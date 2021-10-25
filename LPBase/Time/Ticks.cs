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
    public static long GetCurrent() => Stopwatch.GetTimestamp();

    public static long FromDays(double days) => (long)(days * TicksPerDay);

    public static long FromHours(double hours) => (long)(hours * TicksPerHour);

    public static long FromMinutes(double minutes) => (long)(minutes * TicksPerMinute);

    public static long FromSeconds(double seconds) => (long)(seconds * TicksPerSecond);

    public static long FromMilliseconds(double milliseconds) => (long)(milliseconds * TicksPerMillisecond);

    public static long FromMicroseconds(double microseconds) => (long)(microseconds * TicksPerMicrosecond);

    public static long FromNanoseconds(double nanoseconds) => (long)(nanoseconds * TicksPerNanosecond);
}
