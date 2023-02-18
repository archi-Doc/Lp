// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("zendir", IsSubcommand = true, Description = "Zen directory subcommand")]
public class ZenDirSubcommand : SimpleCommandGroup<ZenDirSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(ZenDirSubcommandLs));
        group.AddCommand(typeof(ZenDirSubcommandAdd));
    }

    public ZenDirSubcommand(UnitContext context, ZenControl control)
        : base(context, "ls")
    {
    }
}
