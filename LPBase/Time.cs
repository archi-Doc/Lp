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

    /// <summary>
    /// Gets the time since LP has started (0001/01/01 0:00:00).<br/>
    /// Not affected by manual date/time changes.
    /// </summary>
    public static DateTime StartupTime
    {
        get
        {
            var ticks = Stopwatch.GetTimestamp() - initialTimestamp;
            return new DateTime(ticks);
        }
    }

    public static double ReciprocalOfFrequency { get; } = 1.0d / Stopwatch.Frequency;

    // private static DateTime initialLocalTime = new DateTime(0);
    private static long initialTimestamp = Stopwatch.GetTimestamp();
}
