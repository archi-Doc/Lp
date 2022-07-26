// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP.Subcommands.Dump;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("flag", IsSubcommand = true)]
public class FlagSubcommand : SimpleCommandGroup<FlagSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(FlagSubcommandOn));
        group.AddCommand(typeof(FlagSubcommandOff));
        group.AddCommand(typeof(FlagSubcommandLs));
        group.AddCommand(typeof(FlagSubcommandClear));
    }

    public FlagSubcommand(UnitContext context)
        : base(context, "ls")
    {
    }
}
