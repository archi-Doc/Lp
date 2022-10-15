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

    public CustomizedCommand()
    {
    }

    public CustomizedCommand(string? command, string[]? commandArray)
    {
        this.Command = command;
        this.CommandArray = commandArray;
    }

    [Key(0)]
    public string? Command { get; private set; } = default!;

    [Key(1)]
    public string[]? CommandArray { get; private set; } = default!;
}
