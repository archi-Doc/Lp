// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace CrystalData.Subcommands;

[SimpleCommand("ctemp", IsSubcommand = true, Description = "Crystal template subcommand")]
public class CrystalTempSubcommand : SimpleCommandGroup<CrystalTempSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(CrystalTempSubcommandLs));
    }

    public CrystalTempSubcommand(UnitContext context, CrystalControl control)
        : base(context)
    {
    }
}
