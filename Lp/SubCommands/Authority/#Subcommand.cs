// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.Authority;

public static class Subcommand
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddCommand(typeof(ListAuthoritySubcommand));
        context.AddCommand(typeof(NewAuthoritySubcommand));
        context.AddCommand(typeof(RemoveAuthoritySubcommand));
        context.AddCommand(typeof(ShowAuthoritySubcommand));
    }
}
