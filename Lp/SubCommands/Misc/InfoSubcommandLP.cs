// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("lp")]
public class InfoSubcommandLp : ISimpleCommand<DumpSubcommandInfoOptions>
{
    public InfoSubcommandLp(LpUnit lpUnit)
    {
        this.LpUnit = lpUnit;
    }

    public async Task Execute(DumpSubcommandInfoOptions options, string[] args, CancellationToken cancellationToken)
    {
        var target = args.Length > 0 ? args[0] : string.Empty;
        var logWriter = this.LpUnit.LogUnit.RootLogService.GetWriter<InfoSubcommandLp>(LogLevel.Information);

        logWriter?.Write($"Info: {target}");

        logWriter?.Write(Environment.OSVersion.ToString());
        logWriter?.Write($"Time.GetApplication(): {Time.GetApplication()}");
        logWriter?.Write($"Time.GetCorrected(): {Time.GetCorrected()}");
    }

    public LpUnit LpUnit { get; set; }
}

public record DumpSubcommandInfoOptions
{
    [SimpleOption("Count", Description = "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Count}";
}
