﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("authority", IsSubcommand = true)]
public class AuthoritySubcommand : SimpleCommandGroup<AuthoritySubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(AuthoritySubcommandLs));
        group.AddCommand(typeof(AuthoritySubcommandNew));
    }

    public AuthoritySubcommand(UnitContext context)
        : base(context)
    {
    }
}