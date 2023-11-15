﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere.Time;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("mics", Description = "Shows mics(microseconds) status")]
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
            logger.Log($"Mics.GetCorrected(): {Mics.GetCorrected()}");
            logger.Log($"Mics.GetCorrected() : {Mics.ToDateTime(Mics.GetCorrected()).ToString()}");
            logger.Log($"Time.TimestampToTicks: {Time.TimestampToTicks}");
            logger.Log($"Time.GetSystem(): {Time.GetSystem()}");
            logger.Log($"Time.GetApplication(): {Time.GetApplication()}");
            logger.Log($"Time.GetUtcNow(): {Time.GetUtcNow()}");
            logger.Log($"Time.GetCorrected(): {Time.GetCorrected()}");
        }
    }

    public Control Control { get; set; }

    private ILogger<MicsSubcommand> logger;
}
