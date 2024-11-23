// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

public static class Subcommand
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(NewVaultSubcommand));
        context.AddSubcommand(typeof(RemoveVaultSubcommand));
        context.AddSubcommand(typeof(ListVaultSubcommand));
        context.AddSubcommand(typeof(ShowVaultSubcommand));
        context.AddSubcommand(typeof(ChangeVaultPasswordSubcommand));
    }
}
