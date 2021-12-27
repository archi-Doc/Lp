// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP;

public struct MicsRange
{
    public MicsRange(long mics)
    {
        this.LowerBound = Mics.GetSystem();
        this.UpperBound = this.LowerBound + mics;
    }

    public static MicsRange FromDays(double days) => new MicsRange((long)(days * Mics.MicsPerDay));

    public static MicsRange FromHours(double hours) => new MicsRange((long)(hours * Mics.MicsPerHour));

    public static MicsRange FromMinutes(double minutes) => new MicsRange((long)(minutes * Mics.MicsPerMinute));

    public static MicsRange FromSeconds(double seconds) => new MicsRange((long)(seconds * Mics.MicsPerSecond));

    public static MicsRange FromMilliseconds(double milliseconds) => new MicsRange((long)(milliseconds * Mics.MicsPerMillisecond));

    public static MicsRange FromMicroseconds(double microseconds) => new MicsRange((long)microseconds);

    public bool IsIn(long mics) => this.LowerBound <= mics && mics <= this.UpperBound;

    public readonly long LowerBound;

    public readonly long UpperBound;
}
