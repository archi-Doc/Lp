// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("on")]
public class FlagsSubcommandOn : ISimpleCommand
{// flags on name
    public FlagsSubcommandOn(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        var flags = this.Control.LPBase.Settings.Flags;
        List<string> on = new();
        List<string> notfound = new();
        foreach (var x in args)
        {
            if (LP.Options.LPFlagsHelper.TrySet(flags, x, true))
            {
                on.Add(x);
            }
            else
            {
                notfound.Add(x);
            }
        }

        if (on.Count > 0)
        {
            Logger.Default.Information($"On: {string.Join(' ', on)}");
        }

        if (notfound.Count > 0)
        {
            Logger.Default.Warning($"Not found: {string.Join(' ', notfound)}");
        }
    }

    public Control Control { get; set; }
}
