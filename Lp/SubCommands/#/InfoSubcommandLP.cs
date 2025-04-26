// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("lp")]
public class InfoSubcommandLp : ISimpleCommand<DumpSubcommandInfoOptions>
{
    public InfoSubcommandLp(Control control)
    {
        this.Control = control;
    }

    public void Run(DumpSubcommandInfoOptions options, string[] args)
    {
        var target = args.Length > 0 ? args[0] : string.Empty;
        var logger = this.Control.UnitLogger.TryGet<InfoSubcommandLp>(LogLevel.Information);

        logger?.Log($"Info: {target}");

        var saa = DateTime.MinValue.Ticks;
        logger?.Log(Environment.OSVersion.ToString());
        logger?.Log($"Time.GetApplication(): {Time.GetApplication()}");
        logger?.Log($"Time.GetCorrected(): {Time.GetCorrected()}");
    }

    public Control Control { get; set; }
}

public record DumpSubcommandInfoOptions
{
    [SimpleOption("Count", Description = "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Count}";
}
