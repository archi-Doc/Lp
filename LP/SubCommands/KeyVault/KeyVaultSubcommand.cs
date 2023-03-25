// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("vault", IsSubcommand = true)]
public class KeyVaultSubcommand : SimpleCommandGroup<KeyVaultSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(KeyVaultSubcommandLs));
        group.AddCommand(typeof(KeyVaultSubcommandChangePass));
        group.AddCommand(typeof(KeyVaultSubcommandAdd));
        group.AddCommand(typeof(KeyVaultSubcommandGet));
        group.AddCommand(typeof(KeyVaultSubcommandDelete));
    }

    public KeyVaultSubcommand(UnitContext context)
        : base(context, "ls")
    {
    }
}
