// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

/// <summary>
/// Flake object requirements.<br/>
/// 1. Inherit IFlake interface.<br/>
/// 2. Has TinyhandObjectAttribute (Tinyhand serializable).<br/>
/// 3. Unique flake id is prefered.<br/>
/// </summary>
public interface IFlake
{
    /// <summary>
    /// Gets the identifier of the flake.
    /// </summary>
    public int FlakeId { get; }
}
