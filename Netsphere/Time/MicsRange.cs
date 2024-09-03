// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public readonly record struct MicsRange
{
    /// <summary>
    /// Creates a <see cref="MicsRange"/> from the present mics (<see cref="Mics.GetCorrected"/>) to the present+specified mics.
    /// </summary>
    /// <param name="duration">Mics ahead from the present mics.</param>
    /// <param name="margin">Allowed margin of error.</param>
    /// <returns><see cref="MicsRange"/>.</returns>
    public static MicsRange FromCorrectedToMics(long duration, long margin = Mics.DefaultMargin)
    {
        var current = Mics.GetCorrected();
        return new MicsRange(current - margin, current + duration + margin);
    }

    public static MicsRange FromFastSystemToFuture(long duration)
    {
        var lower = Mics.FastSystem;
        return new(lower, lower + duration);
    }

    public static MicsRange FromCorrectedToFuture(long duration)
    {
        var lower = Mics.GetCorrected();
        return new(lower, lower + duration);
    }

    public static MicsRange FromPastToFastSystem(long duration)
    {
        var upper = Mics.FastSystem;
        return new(upper - duration, upper);
    }

    public MicsRange(long lowerBoundMics, long upperBoundMics)
    {
        this.LowerBound = lowerBoundMics;
        this.UpperBound = upperBoundMics;
    }

    #region FieldAndProperty

    public readonly long LowerBound;
    public readonly long UpperBound;

    #endregion

    public static MicsRange DaysFromFastSystem(double days)
        => FromFastSystemToFuture((long)(days * Mics.MicsPerDay));

    public static MicsRange HoursFromFastSystem(double hours)
        => FromFastSystemToFuture((long)(hours * Mics.MicsPerHour));

    public static MicsRange MinutesFromFastSystem(double minutes)
        => FromFastSystemToFuture((long)(minutes * Mics.MicsPerMinute));

    public static MicsRange SecondsFromFastSystem(double seconds)
        => FromFastSystemToFuture((long)(seconds * Mics.MicsPerSecond));

    public static MicsRange MillisecondsFromFastSystem(double milliseconds)
        => FromFastSystemToFuture((long)(milliseconds * Mics.MicsPerMillisecond));

    public static MicsRange MicrosecondsFromFastSystem(double microseconds)
        => FromFastSystemToFuture((long)microseconds);

    public bool IsWithin(long targetMics)
        => this.LowerBound <= targetMics && targetMics <= this.UpperBound;

    public bool IsWithinMargin(long targetMics, long margin = Mics.DefaultMargin)
        => (this.LowerBound - margin) <= targetMics && targetMics <= (this.UpperBound + margin);
}
