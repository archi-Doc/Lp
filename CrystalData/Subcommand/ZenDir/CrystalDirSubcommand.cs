// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("cdir", IsSubcommand = true, Description = "Crystal directory subcommand")]
public class CrystalDirSubcommand : SimpleCommandGroup<CrystalDirSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(CrystalDirSubcommandLs));
        group.AddCommand(typeof(CrystalDirSubcommandAdd));
    }

    public CrystalDirSubcommand(UnitContext context, CrystalControl control)
        : base(context, "ls")
    {
    }
}
