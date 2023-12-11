// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public record ServerOptions
{
    /// <summary>
    /// Gets the maximum number of transmissions.
    /// </summary>
    public uint MaxTransmissions { get; init; } = 4;

    /// <summary>
    /// Gets the maximum size of block transmissions.<br/>
    /// 0: Block transmission is disabled.
    /// </summary>
    public uint MaxBlockSize { get; init; } = 4 * 1024 * 1024; // 4MB

    /// <summary>
    /// Gets the maximum size of stream transmissions.<br/>
    /// 0: Stream transmission is disabled.
    /// </summary>
    public ulong MaxStreamSize { get; init; } = 0;
}
