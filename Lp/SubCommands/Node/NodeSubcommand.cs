// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("node", IsSubcommand = true)]
public class NodeSubcommand : SimpleCommandGroup<NodeSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(NodeSubcommandAdd));
        group.AddCommand(typeof(NodeSubcommandLs));
        group.AddCommand(typeof(NodeSubcommandInfo));
    }

    public NodeSubcommand(UnitContext context)
        : base(context, "info")
    {
    }
}
