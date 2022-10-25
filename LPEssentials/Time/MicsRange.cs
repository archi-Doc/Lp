// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP;

public readonly struct MicsRange
{
    /// <summary>
    /// Creates a <see cref="MicsRange"/> from the present mics (<see cref="Mics.GetCorrected"/>) to the present+specified mics.
    /// </summary>
    /// <param name="mics">Mics ahead from the present mics.</param>
    /// <param name="error">Allowable error in mics.</param>
    /// <returns><see cref="MicsRange"/>.</returns>
    public static MicsRange FromCorrectedToMics(long mics, long error)
    {
        var current = Mics.GetCorrected();
        return new MicsRange(current - error, current + mics + error);
    }

    public MicsRange(long mics)
    {
        this.LowerBound = Mics.GetSystem();
        this.UpperBound = this.LowerBound + mics;
    }

    public MicsRange(long lowerBoundMics, long upperBoundMics)
    {
        this.LowerBound = lowerBoundMics;
        this.UpperBound = upperBoundMics;
    }

    public static MicsRange DaysFromNow(double days) => new MicsRange((long)(days * Mics.MicsPerDay));

    public static MicsRange HoursFromNow(double hours) => new MicsRange((long)(hours * Mics.MicsPerHour));

    public static MicsRange MinutesFromNow(double minutes) => new MicsRange((long)(minutes * Mics.MicsPerMinute));

    public static MicsRange SecondsFromNow(double seconds) => new MicsRange((long)(seconds * Mics.MicsPerSecond));

    public static MicsRange MillisecondsFromNow(double milliseconds) => new MicsRange((long)(milliseconds * Mics.MicsPerMillisecond));

    public static MicsRange MicrosecondsFromNow(double microseconds) => new MicsRange((long)microseconds);

    public bool IsIn(long mics) => this.LowerBound <= mics && mics <= this.UpperBound;

    public readonly long LowerBound;

    public readonly long UpperBound;
}
