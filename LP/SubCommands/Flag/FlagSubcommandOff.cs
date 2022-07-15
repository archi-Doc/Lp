// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using LP.Block;
using LP.Data;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands.Dump;

[SimpleCommand("off")]
public class FlagSubcommandOff : ISimpleCommand
{
    public FlagSubcommandOff(Control control)
    {
        this.Control = control;
    }

    public void Run(string[] args)
    {
        var ope = VisceralClass.TryGet(this.Control.LPBase.Settings.Flags);
        if (ope == null)
        {
            return;
        }

        List<string> off = new();
        List<string> notfound = new();
        foreach (var x in args)
        {
            if (ope.TrySet(x, false))
            {
                off.Add(x);
            }
            else
            {
                notfound.Add(x);
            }
        }

        if (off.Count > 0)
        {
            Logger.Default.Information($"Off: {string.Join(' ', off)}");
        }

        if (notfound.Count > 0)
        {
            Logger.Default.Warning($"Not found: {string.Join(' ', notfound)}");
        }
    }

    public Control Control { get; set; }
}
