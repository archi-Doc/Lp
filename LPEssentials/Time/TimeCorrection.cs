// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arc.Collections;
using ValueLink;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP;

[ValueLinkObject]
internal partial class TimeDifference
{
    [Link(Type = ChainType.QueueList, Name = "Queue")]
    internal TimeDifference(long difference)
    {
        this.Difference = difference;
    }

    [Link(Type = ChainType.Ordered)]
    internal long Difference;
}

public enum CorrectedResult
{
    NotCorrected,
    Corrected,
}

public class TimeCorrection
{
    /// <summary>
    /// The maximum number of time corrections.
    /// </summary>
    public const uint MaxCorrections = 1_000;

    /// <summary>
    /// The minimum number of corrections required for a valid corrected ticks/time.
    /// </summary>
    public const uint MinCorrections = 10;

    static TimeCorrection()
    {
        InitialUtcMics = (long)(DateTime.UtcNow.Ticks * Mics.TimestampToMics);
        InitialSystemMics = Mics.GetSystem();

        initialDifference = InitialUtcMics - InitialSystemMics;
        timeCorrections = new();
    }

    public static void Start()
    {// For initialization.
    }

    /// <summary>
    /// Get the corrected number of ticks expressed as UTC.
    /// </summary>
    /// <param name="correctedTicks">The corrected number of ticks.</param>
    /// <returns><see cref="CorrectedResult"/>.</returns>
    public static CorrectedResult GetCorrectedMics(out long correctedTicks)
    {
        var currentTicks = Mics.GetSystem() - InitialSystemMics + InitialUtcMics;
        if (timeCorrections.QueueChain.Count < MinCorrections)
        {
            correctedTicks = currentTicks;
            return CorrectedResult.NotCorrected;
        }

        var difference = GetCollectionDifference(currentTicks);
        correctedTicks = currentTicks + difference;

        return CorrectedResult.Corrected;
    }

    public static void AddCorrection(long utcTicks)
    {
        var difference = utcTicks - (Mics.GetSystem() + initialDifference);

        lock (timeCorrections)
        {
            var c = new TimeDifference(difference);
            c.Goshujin = timeCorrections;

            while (timeCorrections.QueueChain.Count > MaxCorrections)
            {
                if (timeCorrections.QueueChain.TryPeek(out var result))
                {
                    result.Goshujin = null;
                }
            }
        }
    }

    private static long GetCollectionDifference(long currentTicks)
    {
        var diff = correctionDifference;
        if (diff != 0 && System.Math.Abs(currentTicks - correctionTicks) < Mics.FromSeconds(1.0d))
        {
            return diff;
        }

        lock (timeCorrections)
        {// Calculate the average of the differences in the middle half.
            var half = timeCorrections.DifferenceChain.Count >> 1;
            var quarter = timeCorrections.DifferenceChain.Count >> 2;

            var node = timeCorrections.DifferenceChain.First;
            for (var i = 0; i < quarter; i++)
            {
                node = node!.DifferenceLink.Next;
            }

            long total = 0;
            for (var i = 0; i < half; i++)
            {
                total += node!.Difference;
                node = node!.DifferenceLink.Next;
            }

            diff = total / half;
            correctionDifference = diff;
            correctionTicks = currentTicks;
        }

        return diff;
    }

    public static long InitialUtcMics { get; }

    public static long InitialSystemMics { get; }

    private static long initialDifference;
    private static TimeDifference.GoshujinClass timeCorrections;
    private static long correctionDifference;
    private static long correctionTicks;
}
