// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Subcommands;

/// <summary>
/// Immutable credit object.
/// </summary>
[TinyhandObject]
public sealed partial class CustomizedCommand
{
    public const string Prefix = "Custom\\";

    public static string GetName(string name) => Prefix + name;

    public static string[] FromCommandToArray(string command)
    {
        // return command.Trim('\r').Split('\n');
        return command.Split('\n');
    }

    public CustomizedCommand()
    {
    }

    public CustomizedCommand(string command, string[]? args = null)
    {
        if (args != null && args.Length > 0)
        {
            if (!string.IsNullOrEmpty(command))
            {
                command += " ";
            }

            command += string.Join(' ', args);
        }

        this.Command = command.Replace("\\\"", "\"").Replace("\\r\\n", "\n").Replace("\\n", "\n");
    }

    [Key(0)]
    public string Command { get; private set; } = string.Empty;
}
