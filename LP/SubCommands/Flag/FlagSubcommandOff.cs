// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Data;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("off")]
public class FlagSubcommandOff : ISimpleCommand
{
    public FlagSubcommandOff(ILogger<FlagSubcommandOff> logger, Control control)
    {
        this.logger = logger;
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
            this.logger.TryGet()?.Log($"Off: {string.Join(' ', off)}");
        }

        if (notfound.Count > 0)
        {
            this.logger.TryGet(LogLevel.Warning)?.Log($"Not found: {string.Join(' ', notfound)}");
        }
    }

    public Control Control { get; set; }

    private ILogger<FlagSubcommandOff> logger;
}
