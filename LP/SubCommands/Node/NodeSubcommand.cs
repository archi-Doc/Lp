// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP.Subcommands.Dump;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("node", IsSubcommand = true)]
public class NodeSubcommand : SimpleCommandGroup<NodeSubcommand>
{
    public static void Configure(UnitBuilderContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(NodeSubcommandAdd));
    }

    public NodeSubcommand(UnitContext context)
        : base(context)
    {
    }
}
