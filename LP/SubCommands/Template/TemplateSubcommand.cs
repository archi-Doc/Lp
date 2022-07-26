// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("template", IsSubcommand = true)]
public class TemplateSubcommand : SimpleCommandGroup<TemplateSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(TemplateSubcommandLs));
    }

    public TemplateSubcommand(UnitContext context)
        : base(context, null)
    {
    }
}
