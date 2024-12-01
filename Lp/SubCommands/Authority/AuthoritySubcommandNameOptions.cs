// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

public record AuthoritySubcommandNameOptions
{
    [SimpleOption("Name", Description = "Authority name", Required = true)]
    public string AuthorityName { get; init; } = string.Empty;
}
