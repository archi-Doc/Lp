// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP.Subcommands.Dump;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("flags", IsSubcommand = true)]
public class FlagsSubcommand : SimpleCommandGroup<FlagsSubcommand>
{
    public static void Configure(UnitBuilderContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(FlagsSubcommandOn));
        group.AddCommand(typeof(FlagsSubcommandOff));
        group.AddCommand(typeof(FlagsSubcommandLs));
        group.AddCommand(typeof(FlagsSubcommandClear));
    }

    public FlagsSubcommand(UnitContext context)
        : base(context, "ls")
    {
    }
}
