// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("custom", IsSubcommand = true)]
public class CustomSubcommand : SimpleCommandGroup<CustomSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(CustomSubcommandLs));
        group.AddCommand(typeof(CustomSubcommandNew));
        group.AddCommand(typeof(CustomSubcommandRemove));
        group.AddCommand(typeof(CustomSubcommandSet));
        group.AddCommand(typeof(CustomSubcommandInfo));
        group.AddCommand(typeof(CustomSubcommandRun));
    }

    public CustomSubcommand(UnitContext context)
        : base(context, "ls", SimpleCommandLine.SimpleParserOptions.Standard with
        {
            ServiceProvider = context.ServiceProvider,
            RequireStrictCommandName = true,
            RequireStrictOptionName = false,
            DoNotDisplayUsage = true,
            DisplayCommandListAsHelp = true,
        })
    {
    }
}
