// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

public record SimpleVaultOptions
{
    [SimpleOption("name", Description = "Name", Required = true)]
    public string Name { get; init; } = string.Empty;
}
