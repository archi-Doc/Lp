// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace ZenItz.Subcommands;

[SimpleCommand("zentemp", IsSubcommand = true, Description = "Zen template subcommand")]
public class ZenTempSubcommand : SimpleSubcommand<ZenTempSubcommand>
{
    public static void Configure(UnitBuilderContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(ZenTempSubcommandLs));
    }

    public ZenTempSubcommand(UnitContext context, ZenControl control)
        : base(context)
    {
    }
}
