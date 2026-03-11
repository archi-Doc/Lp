// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("off")]
public class FlagSubcommandOff : ISimpleCommand
{
    public FlagSubcommandOff(ILogger<FlagSubcommandOff> logger, LpUnit lpUnit)
    {
        this.logger = logger;
        this.LpUnit = lpUnit;
    }

    public void Run(string[] args)
    {
        var ope = VisceralClass.TryGet(this.LpUnit.LpBase.Settings.Flags);
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
            this.logger.GetWriter()?.Write($"Off: {string.Join(' ', off)}");
        }

        if (notfound.Count > 0)
        {
            this.logger.GetWriter(LogLevel.Warning)?.Write($"Not found: {string.Join(' ', notfound)}");
        }
    }

    public LpUnit LpUnit { get; set; }

    private ILogger<FlagSubcommandOff> logger;
}
