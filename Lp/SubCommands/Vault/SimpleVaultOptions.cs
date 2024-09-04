﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.VaultCommand;

public record SimpleVaultOptions
{
    [SimpleOption("Name", Description = "Name", Required = true)]
    public string Name { get; init; } = string.Empty;
}
