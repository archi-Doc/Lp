// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands.CrystalData;

[SimpleCommand("storage", IsSubcommand = true, Description = "Crystal storage subcommand")]
public class CrystalStorageSubcommand : SimpleCommandGroup<CrystalStorageSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(CrystalStorageSubcommandLs));
        group.AddCommand(typeof(CrystalStorageSubcommandAdd));
        group.AddCommand(typeof(CrystalStorageSubcommandDelete));
    }

    public CrystalStorageSubcommand(UnitContext context, CrystalControl control)
        : base(context, "ls")
    {
    }
}
