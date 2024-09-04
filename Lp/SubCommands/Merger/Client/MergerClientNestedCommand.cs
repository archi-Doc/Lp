// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Subcommands;

public class MergerClientNestedCommand : NestedCommand<MergerClientNestedCommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var t = typeof(MergerClientNestedCommand);
        context.TryAddSingleton(t);

        var group = context.GetCommandGroup(t);
        group.AddCommand(typeof(MergerNestedcommandInfo));
        group.AddCommand(typeof(MergerNestedcommandCreateCredit));
    }

    public MergerClientNestedCommand(UnitContext context, UnitCore core, IUserInterfaceService userInterfaceService)
        : base(context, core, userInterfaceService)
    {
    }

    public override string Prefix => "merger-client >> ";

    public NetNode Node { get; set; } = NetNode.Alternative;
}
