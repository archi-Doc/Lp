// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    public const string Prefix = "Command\\";

    public record ExecuteOptions
    {
        [SimpleOption("Name", Description = "Command name", Required = true)]
        public string Name { get; init; } = string.Empty;
    }

    public record Options
    {
        [SimpleOption("Name", Description = "Command name", Required = true)]
        public string Name { get; init; } = string.Empty;

        [SimpleOption("Command", Description = "Command content. To specify multiple commands, use the multiline delimiter \"\"\".", Required = true)]
        public string Command { get; init; } = string.Empty;
    }

    public static string GetName(string name) => Prefix + name;

    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(ListCommand));
        context.AddSubcommand(typeof(NewCommand));
        context.AddSubcommand(typeof(ChangeCommandGroup));
        context.AddSubcommand(typeof(ExecuteCommand));
        context.AddSubcommand(typeof(ShowCommand));
        context.AddSubcommand(typeof(RemoveCommand));
    }

    public static void ShowCommands(string[] commands, ILogger logger)
    {
        foreach (var x in commands)
        {
            logger.GetWriter()?.Write(x);
        }
    }
}
