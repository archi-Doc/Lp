// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("vault", IsSubcommand = true)]
public class VaultSubcommand : SimpleCommandGroup<VaultSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(VaultSubcommandLs));
        group.AddCommand(typeof(VaultSubcommandChangePass));
        group.AddCommand(typeof(VaultSubcommandAdd));
        group.AddCommand(typeof(VaultSubcommandGet));
        group.AddCommand(typeof(VaultSubcommandDelete));
    }

    public VaultSubcommand(UnitContext context)
        : base(context, "ls")
    {
    }
}
