// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP.Subcommands.Dump;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("dump", IsSubcommand = true)]
public class DumpSubcommand : SimpleCommandGroup<DumpSubcommand>
{
    public static void Configure(UnitBuilderContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(DumpSubcommandInfo));
        group.AddCommand(typeof(DumpSubcommandOptions));
    }

    public DumpSubcommand(UnitContext context)
        : base(context, "info")
    {
    }
}
