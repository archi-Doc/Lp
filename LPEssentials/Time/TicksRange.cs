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
        this.LowerBound = Nsec.GetSystem();
        this.UpperBound = this.LowerBound + ticks;
    }

    public static TicksRange FromDays(double days) => new TicksRange((long)(days * Nsec.NsecPerDay));

    public static TicksRange FromHours(double hours) => new TicksRange((long)(hours * Nsec.NsecPerHour));

    public static TicksRange FromMinutes(double minutes) => new TicksRange((long)(minutes * Nsec.NsecPerMinute));

    public static TicksRange FromSeconds(double seconds) => new TicksRange((long)(seconds * Nsec.NsecPerSecond));

    public static TicksRange FromMilliseconds(double milliseconds) => new TicksRange((long)(milliseconds * Nsec.NsecPerMillisecond));

    public static TicksRange FromMicroseconds(double microseconds) => new TicksRange((long)(microseconds * Nsec.NsecPerMicrosecond));

    public static TicksRange FromNanoseconds(double nanoseconds) => new TicksRange((long)nanoseconds);

    public bool IsIn(long ticks) => this.LowerBound <= ticks && ticks <= this.UpperBound;

    public readonly long LowerBound;

    public readonly long UpperBound;
}
