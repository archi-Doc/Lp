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
    public MicsSubcommand(ILogger<MicsSubcommand> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public void Run(string[] args)
    {
        var logger = this.logger.TryGet();
        if (logger != null)
        {
            logger.Log($"Stopwatch.Frequency: {Stopwatch.Frequency}");
            logger.Log($"Mics.TimestampToMics: {Mics.TimestampToMics}");
            logger.Log($"Mics.GetSystem(): {Mics.GetSystem()}");
            logger.Log($"Mics.GetApplication(): {Mics.GetApplication()}");
            logger.Log($"Mics.GetUtcNow(): {Mics.GetUtcNow()}");
            Mics.GetCorrected(out var correctedMics);
            logger.Log($"Mics.GetCorrected(): {correctedMics}");
            logger.Log($"Time.TimestampToTicks: {Time.TimestampToTicks}");
            logger.Log($"Time.GetSystem(): {Time.GetSystem()}");
            logger.Log($"Time.GetApplication(): {Time.GetApplication()}");
            logger.Log($"Time.GetUtcNow(): {Time.GetUtcNow()}");
            Time.GetCorrected(out var correctedTime);
            logger.Log($"Time.GetCorrected(): {correctedTime}");
            logger.Log($"Time.GetCorrected().ToLocalTime(): {correctedTime.ToLocalTime()}");
        }
    }

    public Control Control { get; set; }

    private ILogger<MicsSubcommand> logger;
}
