// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

// [SimpleCommand("mergernested", IsSubcommand = true)]
public class MergerNestedcommand : Nestedcommand<MergerNestedcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var t = typeof(MergerNestedcommand);
        context.TryAddSingleton(t);

        var group = context.GetCommandGroup(t);
        // var group = ConfigureGroup(context);
        group.AddCommand(typeof(MergerNestedcommandInfo));
        group.AddCommand(typeof(MergerNestedcommandCreateCredit));
    }

    public MergerNestedcommand(UnitContext context, UnitCore core, IUserInterfaceService userInterfaceService)
        : base(context, core, userInterfaceService)
    {
    }

    public override string Prefix => "merger >> "; // $"{this.Node.ToShortString()} >> ";

    public NodeInformation Node { get; set; } = NodeInformation.Alternative;
}
