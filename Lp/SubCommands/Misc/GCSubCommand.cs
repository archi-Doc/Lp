// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("gc")]
public class GCSubcommand : ISimpleCommand
{
    public GCSubcommand(ILogger<GCSubcommand> logger, LpUnit lpUnit)
    {
        this.logger = logger;
        this.LpUnit = lpUnit;
    }

    public async Task Execute(string[] args, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write(Hashed.Subcommands.GC.Start);
        GC.Collect();
    }

    public LpUnit LpUnit { get; set; }

    private ILogger<GCSubcommand> logger;
}
