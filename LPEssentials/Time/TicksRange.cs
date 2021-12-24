// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP;

public struct TicksRange
{
    public TicksRange(long ticks)
    {
        this.LowerBound = Mics.GetSystem();
        this.UpperBound = this.LowerBound + ticks;
    }

    public static TicksRange FromDays(double days) => new TicksRange((long)(days * Mics.MicsPerDay));

    public static TicksRange FromHours(double hours) => new TicksRange((long)(hours * Mics.MicsPerHour));

    public static TicksRange FromMinutes(double minutes) => new TicksRange((long)(minutes * Mics.MicsPerMinute));

    public static TicksRange FromSeconds(double seconds) => new TicksRange((long)(seconds * Mics.MicsPerSecond));

    public static TicksRange FromMilliseconds(double milliseconds) => new TicksRange((long)(milliseconds * Mics.MicsPerMillisecond));

    public static TicksRange FromMicroseconds(double microseconds) => new TicksRange((long)microseconds);

    public bool IsIn(long ticks) => this.LowerBound <= ticks && ticks <= this.UpperBound;

    public readonly long LowerBound;

    public readonly long UpperBound;
}
