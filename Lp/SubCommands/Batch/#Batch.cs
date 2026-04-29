// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class Batch
{
    public const string Prefix = "Batch\\";

    public record ExecuteOptions
    {
        [SimpleOption("Name", Description = "Batch name", Required = true)]
        public string Name { get; init; } = string.Empty;
    }

    public record Options
    {
        [SimpleOption("Name", Description = "Batch name", Required = true)]
        public string Name { get; init; } = string.Empty;

        [SimpleOption("Command", Description = "Command content. To specify multiple commands, use the multiline delimiter \"\"\".", Required = true)]
        public string Command { get; init; } = string.Empty;
    }

    public static string GetName(string name) => Prefix + name;

    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(ListBatch));
        context.AddSubcommand(typeof(NewBatch));
        context.AddSubcommand(typeof(ChangeBatch));
        context.AddSubcommand(typeof(ExecuteBatch));
        context.AddSubcommand(typeof(ShowBatch));
        context.AddSubcommand(typeof(RemoveBatch));
    }

    public static void ShowCommands(string[] commands, ILogger logger)
    {
        foreach (var x in commands)
        {
            logger.GetWriter()?.Write(x);
        }
    }
}
