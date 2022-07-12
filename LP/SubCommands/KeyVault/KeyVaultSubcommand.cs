// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("keyvault", IsSubcommand = true)]
public class KeyVaultSubcommand : SimpleCommandGroup<KeyVaultSubcommand>
{
    public static void Configure(UnitBuilderContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(KeyVaultSubcommandLs));
        group.AddCommand(typeof(KeyVaultSubcommandChangePass));
    }

    public KeyVaultSubcommand(UnitContext context)
        : base(context)
    {
    }
}
