// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ZenItz;

/// <summary>
/// Flake class requirements.<br/>
/// 1. Inherit IFlake interface.<br/>
/// 2. Has TinyhandObjectAttribute (Tinyhand serializable).<br/>
/// 3. Unique flake id is prefered.<br/>
/// </summary>
public interface IFlake
{
    /// <summary>
    /// Gets the identifier of the block.
    /// </summary>
    public int FlakeId { get; }
}
