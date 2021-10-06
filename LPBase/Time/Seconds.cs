// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LP;

public static class Seconds
{
    public const double SecondsPerDay = 86400d;
    public const double SecondsPerHour = 3600d;
    public const double SecondsPerMinute = 60d;
    public const double SecondsPerMillisecond = 0.001d;
    public const double SecondsPerMicrosecond = 0.000001d;
    public const double SecondsPerNanosecond = 0.000000001d;

    public static double GetCurrent() => Stopwatch.GetTimestamp() * ReverseFrequency;

    public static double FromDays(double days) => days * SecondsPerDay;

    public static double FromHours(double hours) => hours * SecondsPerHour;

    public static double FromMinutes(double minutes) => minutes * SecondsPerMinute;

    public static double FromSeconds(double seconds) => seconds;

    public static double FromMilliseconds(double milliseconds) => milliseconds * SecondsPerMillisecond;

    public static double FromMicroseconds(double microseconds) => microseconds * SecondsPerMicrosecond;

    public static double FromNanoseconds(double nanoseconds) => nanoseconds * SecondsPerNanosecond;

    private static double ReverseFrequency { get; } = 1.0d / Stopwatch.Frequency;
}
