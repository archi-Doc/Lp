// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP.Net;
using SimpleCommandLine;

namespace LP;

public record LPConsoleOptions
{
    [SimpleOption("mode", description: "LP mode (merger, user)")]
    public string Mode { get; set; } = string.Empty;

    [SimpleOption("directory", description: "Root directory")]
    public string Directory { get; set; } = string.Empty;

    [SimpleOption("ns", description: "Netsphere option")]
    public NetsphereOptions NetsphereOptions { get; set; } = default!;

    public override string ToString()
    {
        return $"{this.NetsphereOptions.ToString()}";
    }
}
