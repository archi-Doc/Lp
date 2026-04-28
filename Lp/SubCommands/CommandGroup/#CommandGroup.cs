// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    public const string Prefix = "CommandGroup\\";

    public record ExecuteOptions
    {
        [SimpleOption("Name", Description = "Command group name", Required = true)]
        public string Name { get; init; } = string.Empty;
    }

    public record Options
    {
        [SimpleOption("Name", Description = "Command group name", Required = true)]
        public string Name { get; init; } = string.Empty;

        [SimpleOption("Command", Description = "Command content. To specify multiple commands, use the multiline delimiter \"\"\".", Required = true)]
        public string Command { get; init; } = string.Empty;
    }

    public static string GetName(string name) => Prefix + name;

    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(ListCommandGroup));
        context.AddSubcommand(typeof(NewCommandGroup));
        context.AddSubcommand(typeof(ChangeCommandGroup));
        context.AddSubcommand(typeof(ExecuteCommandGroup));
        context.AddSubcommand(typeof(ShowCommandGroup));
        context.AddSubcommand(typeof(RemoveCommandGroup));
    }

    public static void ShowCommands(string[] commands, ILogger logger)
    {
        foreach (var x in commands)
        {
            logger.GetWriter()?.Write(x);
        }
    }
}
