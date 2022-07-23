// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("gc")]
public class GCSubcommand : ISimpleCommand
{
    public GCSubcommand(ILogger<GCSubcommand> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public void Run(string[] args)
    {
        this.logger.TryGet()?.Log(Hashed.Subcommands.GC.Start);
        GC.Collect();
    }

    public Control Control { get; set; }

    private ILogger<GCSubcommand> logger;
}
