// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("nodekey", IsSubcommand = true)]
public class NodeKeySubcommand : SimpleCommandGroup<NodeKeySubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(NodeKeySubcommandInfo));
        group.AddCommand(typeof(NodeKeySubcommandNew));
    }

    public NodeKeySubcommand(UnitContext context)
        : base(context, "info")
    {
    }
}
