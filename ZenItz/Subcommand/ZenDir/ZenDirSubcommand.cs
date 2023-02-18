// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("zendir", IsSubcommand = true, Description = "Zen directory subcommand")]
public class CrystalDirSubcommand : SimpleCommandGroup<CrystalDirSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(ZenDirSubcommandLs));
        group.AddCommand(typeof(ZenDirSubcommandAdd));
    }

    public CrystalDirSubcommand(UnitContext context, CrystalControl control)
        : base(context, "ls")
    {
    }
}
