// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("exit")]
public class ExitSubcommand : ISimpleCommand
{
    public ExitSubcommand(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
    }

    public Control Control { get; set; }
}
