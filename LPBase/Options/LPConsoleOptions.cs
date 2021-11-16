// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP.Net;
using SimpleCommandLine;

namespace LP;

public record LPConsoleOptions
{
    [SimpleOption("development", description: "Development")]
    public bool Development { get; init; } = false;

    [SimpleOption("mode", description: "LP mode (relay, merger, user)")]
    public string Mode { get; init; } = string.Empty;

    [SimpleOption("directory", description: "Root directory")]
    public string Directory { get; init; } = string.Empty;

    [SimpleOption("name", description: "Node name")]
    public string NodeName { get; init; } = string.Empty;

    [SimpleOption("ns", description: "Netsphere option")]
    public NetsphereOptions NetsphereOptions { get; init; } = default!;

    public override string ToString()
    {
        return $"{this.NetsphereOptions.ToString()}";
    }
}
