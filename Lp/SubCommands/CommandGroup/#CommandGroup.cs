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

    /// <summary>
    /// Splits the input character span into an array of strings, with each string representing a line.
    /// </summary>
    /// <param name="source">The read-only character span to split into lines.</param>
    /// <returns>
    /// An array of strings where each element represents a line from the input.
    /// Empty lines are preserved in the result.
    /// </returns>
    /// <remarks>
    /// This method handles both Unix-style ('\n') and Windows-style ('\r\n') line endings.
    /// The line ending characters are not included in the resulting strings.
    /// </remarks>
    public static string[] SplitLines(ReadOnlySpan<char> source)
    {
        var count = 1;
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == '\n')
            {
                count++;
            }
        }

        var result = new string[count];
        var resultIndex = 0;
        var start = 0;
        while (true)
        {
            var relativeLf = source[start..].IndexOf('\n');
            if (relativeLf < 0)
            {
                break;
            }

            var lf = start + relativeLf;
            var end = lf;
            if (end > start && source[end - 1] == '\r')
            {
                end--;
            }

            result[resultIndex++] = source[start..end].ToString();
            start = lf + 1;
        }

        result[resultIndex] = source[start..].ToString();
        return result;
    }
}
