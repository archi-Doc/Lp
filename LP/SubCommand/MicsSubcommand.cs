// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Text;
using Arc.Crypto;
using LP;
using LP.Block;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("mics")]
public class MicsSubcommand : ISimpleCommand
{
    public MicsSubcommand(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        var logger = Logger.Priority;

        logger.Information($"Stopwatch.Frequency: {Stopwatch.Frequency}");
        logger.Information($"Mics.TimestampToMics: {Mics.TimestampToMics}");
        logger.Information($"Mics.GetSystem(): {Mics.GetSystem()}");
        logger.Information($"Mics.GetApplication(): {Mics.GetApplication()}");
        logger.Information($"Mics.GetUtcNow(): {Mics.GetUtcNow()}");
        Mics.GetCorrected(out var correctedMics);
        logger.Information($"Mics.GetCorrected(): {correctedMics}");
        logger.Information($"Time.TimestampToTicks: {Time.TimestampToTicks}");
        logger.Information($"Time.GetSystem(): {Time.GetSystem()}");
        logger.Information($"Time.GetApplication(): {Time.GetApplication()}");
        logger.Information($"Time.GetUtcNow(): {Time.GetUtcNow()}");
        Time.GetCorrected(out var correctedTime);
        logger.Information($"Time.GetCorrected(): {correctedTime}");
        logger.Information($"Time.GetCorrected().ToLocalTime(): {correctedTime.ToLocalTime()}");
    }

    public Control Control { get; set; }
}
