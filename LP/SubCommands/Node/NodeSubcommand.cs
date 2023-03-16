// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("node", IsSubcommand = true)]
public class NodeSubcommand : SimpleCommandGroup<NodeSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(NodeSubcommandAdd));
        group.AddCommand(typeof(NodeSubcommandLs));
        group.AddCommand(typeof(NodeSubcommandInfo));
        group.AddCommand(typeof(NodeSubcommandGet));
    }

    public NodeSubcommand(UnitContext context)
        : base(context, "info")
    {
    }
}
