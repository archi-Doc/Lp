// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Crypto;
using ValueLink;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace LP;

[ValueLinkObject]
internal partial class TimeCorrection
{
    [Link(Type = ChainType.QueueList, Name = "Queue")]
    internal TimeCorrection(long difference)
    {
        this.Difference = difference;
    }

    [Link(Type = ChainType.Ordered)]
    internal long Difference;
}

public static class Time
{
    /// <summary>
    /// The maximum number of time corrections.
    /// </summary>
    public const uint MaxCorrections = 1_000;

    /// <summary>
    /// The minimum number of corrections required for a valid corrected time.
    /// </summary>
    public const uint MinCorrections = 10;

    static Time()
    {
        initialUtcTime = DateTime.UtcNow;
        initialTimestamp = Stopwatch.GetTimestamp();
        initialUtcDifference = initialUtcTime.Ticks - initialTimestamp;
        timeCorrections = new();
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

    public enum CorrectedTimeResult
    {
        NotCorrected,
        Corrected,
    }

    /// <summary>
    /// Get the corrected <see cref="DateTime"/> expressed as UTC.
    /// </summary>
    /// <param name="correctedTime">The corrected time.</param>
    /// <returns><see cref="CorrectedTimeResult"/>.</returns>
    public static CorrectedTimeResult GetCorrectedTime(out DateTime correctedTime)
    {
        var currentTicks = Stopwatch.GetTimestamp() - initialTimestamp + initialUtcTime.Ticks;
        if (timeCorrections.QueueChain.Count() < MinCorrections)
        {
            correctedTime = new DateTime(currentTicks);
            return CorrectedTimeResult.NotCorrected;
        }

        var difference = GetCollectionDifference(currentTicks);
        correctedTime = new DateTime(currentTicks + difference);

        return CorrectedTimeResult.Corrected;
    }

    public static void AddTimeForCorrection(long utcTicks)
    {
        var difference = utcTicks - (Stopwatch.GetTimestamp() + initialUtcDifference);

        lock (timeCorrections)
        {
            var c = new TimeCorrection(difference);
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

    private static long GetCollectionDifference(long current)
    {
        var diff = correctionDifference;
        if (diff != 0 && System.Math.Abs(current - correctionTimestamp) < Ticks.FromSeconds(1.0d))
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
            correctionTimestamp = current;
        }

        return diff;
    }

    public static double ReciprocalOfFrequency { get; } = 1.0d / Stopwatch.Frequency;

    private static DateTime initialUtcTime;
    private static long initialUtcDifference;
    private static long initialTimestamp;

    private static TimeCorrection.GoshujinClass timeCorrections;
    private static long correctionDifference;
    private static long correctionTimestamp;
}
