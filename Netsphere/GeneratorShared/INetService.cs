// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

/// <summary>
/// A base interface for net service.
/// </summary>
public interface INetService
{
    public NetResult Result => NetResult.Success;

    public bool IsSuccess => this.Result == NetResult.Success;
}
