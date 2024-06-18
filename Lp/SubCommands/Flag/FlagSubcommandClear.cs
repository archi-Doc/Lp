// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Data;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("clear")]
public class FlagSubcommandClear : ISimpleCommand
{
    public FlagSubcommandClear(ILogger<FlagSubcommandClear> logger, Control control)
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

        List<string> cleared = new();
        var names = ope.GetNames();
        foreach (var x in names)
        {
            if (ope.TryGet<bool>(x, out var value))
            {
                if (value)
                {
                    ope.TrySet(x, false);
                    cleared.Add(x);
                }
            }
        }

        if (cleared.Count > 0)
        {
            this.logger.TryGet()?.Log($"Cleared: {string.Join(' ', cleared)}");
        }
    }

    public Control Control { get; set; }

    private ILogger<FlagSubcommandClear> logger;
}
