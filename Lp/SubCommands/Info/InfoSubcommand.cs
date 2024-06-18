// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("info", IsSubcommand = true)]
public class InfoSubcommand : SimpleCommandGroup<InfoSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(InfoSubcommandLP));
    }

    public InfoSubcommand(UnitContext context)
        : base(context, "lp")
    {
    }
}
