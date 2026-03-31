// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("mics", Description = "Shows mics(microseconds) status")]
public class MicsSubcommand : ISimpleCommand
{
    public MicsSubcommand(ILogger<MicsSubcommand> logger, LpUnit lpUnit)
    {
        this.logger = logger;
        this.LpUnit = lpUnit;
    }

    public void Run(string[] args)
    {
        var logWriter = this.logger.GetWriter();
        if (logWriter.HasValue)
        {
            var writer = logWriter.Value;
            writer.Write($"Stopwatch.Frequency: {Stopwatch.Frequency}");
            writer.Write($"Mics.TimestampToMics: {Mics.TimestampToMics}");
            writer.Write($"Mics.GetSystem(): {Mics.GetSystem()}");
            writer.Write($"Mics.FastSystem: {Mics.FastSystem}");
            writer.Write($"Mics.GetApplication(): {Mics.GetApplication()}");
            writer.Write($"Mics.FastApplication: {Mics.FastApplication}");
            writer.Write($"Mics.GetUtcNow(): {Mics.GetUtcNow()}");
            writer.Write($"Mics.FastUtcNow: {Mics.FastUtcNow}");
            writer.Write($"Mics.GetCorrected(): {Mics.GetCorrected()}");
            writer.Write($"Mics.FastCorrected: {Mics.FastCorrected}");
            writer.Write($"Mics.GetCorrected() : {Mics.GetCorrected().MicsToDateTimeString()}");
            writer.Write($"Time.TimestampToTicks: {Time.TimestampToTicks}");
            writer.Write($"Time.GetSystem(): {Time.GetSystem()}");
            writer.Write($"Time.GetApplication(): {Time.GetApplication()}");
            writer.Write($"Time.GetUtcNow(): {Time.GetUtcNow()}");
            writer.Write($"Time.GetCorrected(): {Time.GetCorrected()}");
        }
    }

    public LpUnit LpUnit { get; set; }

    private ILogger<MicsSubcommand> logger;
}
