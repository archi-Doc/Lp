// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("on")]
public class FlagSubcommandOn : ISimpleCommand
{
    public FlagSubcommandOn(ILogger<FlagSubcommandOn> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public void Run(string[] args)
    {
        var ope = VisceralClass.TryGet(this.Control.LpBase.Settings.Flags);
        if (ope == null)
        {
            return;
        }

        List<string> on = new();
        List<string> notfound = new();
        foreach (var x in args)
        {
            if (ope.TrySet(x, true))
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
            this.logger.TryGet()?.Log($"On: {string.Join(' ', on)}");
        }

        if (notfound.Count > 0)
        {
            this.logger.TryGet(LogLevel.Warning)?.Log($"Not found: {string.Join(' ', notfound)}");
        }
    }

    public Control Control { get; set; }

    private ILogger<FlagSubcommandOn> logger;
}
