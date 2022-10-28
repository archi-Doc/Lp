// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("nodekey", IsSubcommand = true)]
public class NodeKeySubcommand : SimpleCommandGroup<NodeKeySubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(NodeKeySubcommandInfo));
        group.AddCommand(typeof(NodeKeySubcommandNew));
        group.AddCommand(typeof(NodeKeySubcommandSet));
    }

    public NodeKeySubcommand(UnitContext context)
        : base(context, "info")
    {
    }
}
