// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public record ServerOptions
{
    public uint MaxTransmissions { get; init; } = 4;

    public uint MaxBlockSize { get; init; } = 4 * 1024 * 1024; // 4MB
}
