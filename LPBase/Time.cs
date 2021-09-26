// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Crypto;

namespace LP;

public static class Time
{
    static Time()
    {
    }

    public static DateTime LocalTime
    {
        get
        {
            var ticks = Stopwatch.GetTimestamp() - initialTimestamp;
            return initialLocalTime + new TimeSpan(ticks);
        }
    }

    public static double ReciprocalOfFrequency { get; } = 1.0d / Stopwatch.Frequency;

    private static DateTime initialLocalTime = DateTime.Now;
    private static long initialTimestamp = Stopwatch.GetTimestamp();
}
