﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("export", IsSubcommand = true)]
public class ExportSubcommand : SimpleCommandGroup<ExportSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(ExportSubcommandOptions));
    }

    public ExportSubcommand(UnitContext context)
        : base(context, null)
    {
    }
}
