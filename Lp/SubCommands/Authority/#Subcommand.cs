// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

public static class Subcommand
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(ListAuthoritySubcommand));
        context.AddSubcommand(typeof(NewAuthoritySubcommand));
        context.AddSubcommand(typeof(RemoveAuthoritySubcommand));
        context.AddSubcommand(typeof(ShowAuthoritySubcommand));
    }
}
