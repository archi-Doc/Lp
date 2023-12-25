// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

/// <summary>
/// Represents a net response.<br/>
/// <see cref="NetResult.Success"/>: <see cref="NetResponse.Received"/> is valid, and it's preferable to call Return() method.<br/>
/// Other: <see cref="NetResponse.Received"/> is default (empty).
/// </summary>
public readonly record struct NetResponse
{
    public NetResponse(NetResult result, ByteArrayPool.MemoryOwner received, int elapsedMics)
    {
        this.Result = result;
        this.Received = received;
        this.ElapsedMics = elapsedMics;
    }

    public NetResponse(NetResult result)
    {
        this.Result = result;
    }

    public bool IsFailure => this.Result != NetResult.Success;

    public bool IsSuccess => this.Result == NetResult.Success;

    public void Return() => this.Received.Return();

    public readonly NetResult Result;
    public readonly ByteArrayPool.MemoryOwner Received;
    public readonly int ElapsedMics;
}
