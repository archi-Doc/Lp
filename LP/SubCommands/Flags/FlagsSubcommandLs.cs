// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("ls")]
public class FlagsSubcommandLs : ISimpleCommand
{// flags on name
    public FlagsSubcommandLs(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        var names = LP.Data.LPFlagsHelper.GetNames();
        if (names.Length > 0)
        {
            Logger.Default.Information($"Flags: {string.Join(' ', names)}");
        }
    }

    public Control Control { get; set; }
}
