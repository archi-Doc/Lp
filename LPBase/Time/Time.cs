// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LP;

public static class Time
{
    /// <summary>
    /// Gets a DateTime since system startup (Stopwatch.GetTimestamp()).
    /// </summary>
    /// <returns><see cref="DateTime"/>.</returns>
    public static DateTime GetSystem() => new DateTime(Ticks.GetSystem());

    /// <summary>
    /// Gets a <see cref="DateTime"/> since LP has started (0001/01/01 0:00:00).<br/>
    /// Not affected by manual date/time changes.
    /// </summary>
    /// <returns><see cref="DateTime"/>.</returns>
    public static DateTime GetApplication() => new DateTime(Ticks.GetApplication());

    /// <summary>
    /// Gets a <see cref="DateTime"/> expressed as UTC.
    /// </summary>
    /// <returns><see cref="DateTime"/>.</returns>
    public static DateTime GetUtcNow() => DateTime.UtcNow;

    /// <summary>
    /// Get a corrected <see cref="DateTime"/> expressed as UTC.
    /// </summary>
    /// <param name="correctedTime">The corrected time.</param>
    /// <returns><see cref="CorrectedResult"/>.</returns>
    public static CorrectedResult GetCorrected(out DateTime correctedTime)
    {
        var result = TimeCorrection.GetCorrectedTicks(out var ticks);
        correctedTime = new DateTime(ticks);
        return result;
    }
}
