// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("remote", IsSubcommand = true)]
public class RemoteSubcommand : SimpleCommandGroup<RemoteSubcommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var group = ConfigureGroup(context);
        group.AddCommand(typeof(RemoteSubcommandRestart));
    }

    public RemoteSubcommand(UnitContext context)
        : base(context)
    {
    }
}
