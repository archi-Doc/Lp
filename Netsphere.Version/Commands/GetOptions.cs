// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Netsphere.Version;

public partial record GetOptions
{
    [SimpleOption("address", Description = "Target address")]
    public string Address { get; init; } = string.Empty;
}
