// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Text;
using Arc.Crypto;
using LP;
using LP.Block;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("nsec")]
public class NsecSubcommand : ISimpleCommand
{
    public NsecSubcommand(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        var logger = Logger.Priority;

        logger.Information($"Stopwatch.Frequency: {Stopwatch.Frequency}");
        logger.Information($"Nsec.GetSystem(): {Nsec.GetSystem()}");
        logger.Information($"Time.GetSystem(): {Time.GetSystem()}");
        logger.Information($"Time.GetUtcNow(): {Time.GetUtcNow()}");
        Time.GetCorrected(out var correctedTime);
        logger.Information($"Time.GetCorrected(): {correctedTime}");
        logger.Information($"Time.GetCorrected().ToLocalTime(): {correctedTime.ToLocalTime()}");
    }

    public Control Control { get; set; }
}
