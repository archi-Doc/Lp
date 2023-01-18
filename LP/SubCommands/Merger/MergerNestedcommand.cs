// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

// [SimpleCommand("mergernested", IsSubcommand = true)]
public class MergerNestedcommand : Nestedcommand<MergerNestedcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.TryAddSingleton(typeof(MergerNestedcommand));
        // var group = ConfigureGroup(context);
        // group.AddCommand(typeof(RemoteSubcommandRestart));
    }

    public MergerNestedcommand(UnitContext context, UnitCore core, IUserInterfaceService userInterfaceService)
        : base(context, core, userInterfaceService)
    {
    }
}
