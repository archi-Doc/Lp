// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using LP.Net;
using SimpleCommandLine;

namespace LP;

public record LPConsoleOptions
{
    [SimpleOption("ns", description: "Netsphere option")]
    public NetsphereOptions NetsphereOptions { get; set; } = default!;

    [SimpleOption("mode", description: "LP mode (merger, user)")]
    public string Mode { get; set; } = string.Empty;
}
