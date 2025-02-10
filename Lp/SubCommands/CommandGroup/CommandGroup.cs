// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Subcommands;

public partial class CommandGroup
{
    public const string Prefix = "CommandGroup\\";

    public static string GetName(string name) => Prefix + name;

    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(ListCommand));
        context.AddSubcommand(typeof(NewCommand));
        context.AddSubcommand(typeof(SetCommand));
        context.AddSubcommand(typeof(ExecuteCommand));
    }

    public static void ShowCommands(string[] commands, ILogger logger)
    {
        foreach (var x in commands)
        {
            logger.TryGet()?.Log(x);
        }
    }
}
