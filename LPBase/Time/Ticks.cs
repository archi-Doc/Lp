// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP;

public struct Ticks
{
    public static long FromDays(double days) => (long)(days * TimeSpan.TicksPerDay);

    public static long FromHours(double hours) => (long)(hours * TimeSpan.TicksPerHour);

    public static long FromMinutes(double minutes) => (long)(minutes * TimeSpan.TicksPerMinute);

    public static long FromSeconds(double seconds) => (long)(seconds * TimeSpan.TicksPerSecond);

    public static long FromMilliseconds(double milliseconds) => (long)(milliseconds * TimeSpan.TicksPerMillisecond);
}
