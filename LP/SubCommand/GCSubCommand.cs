// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("gc")]
public class GCSubCommand : ISimpleCommand
{
    public GCSubCommand(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        Logger.Subcommand.Information($"GC.Collect");
        GC.Collect();
    }

    public Control Control { get; set; }
}
