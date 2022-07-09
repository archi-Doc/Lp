// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP.Subcommands.Dump;
using LP.Unit;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("dump", IsSubcommand = true)]
public class DumpSubcommand : SimpleSubcommand<DumpSubcommand>
{
    public static void Configure(UnitBuilderContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(DumpSubcommandInfo));
        group.AddCommand(typeof(DumpSubcommandOptions));
    }

    public DumpSubcommand(UnitParameter parameter)
        : base(parameter, "info")
    {
    }
}
