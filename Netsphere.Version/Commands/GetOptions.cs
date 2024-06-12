// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Netsphere.Version;

public partial record GetOptions
{
    [SimpleOption("node", Description = "Target node")]
    public string Node { get; init; } = string.Empty;
}
