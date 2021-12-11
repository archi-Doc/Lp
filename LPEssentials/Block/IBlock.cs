// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LP.Block;

/// <summary>
/// Block class requirements.<br/>
/// 1. Inherit IBlock interface.<br/>
/// 2. Has TinyhandObjectAttribute (Tinyhand serializable).<br/>
/// 3. Unique block id is prefered.<br/>
/// 4. Length of serialized byte array is less than or equal to <see cref="BlockService.MaxBlockSize"/>.
/// </summary>
public interface IBlock
{
    /// <summary>
    /// Gets an identifier of the block.
    /// </summary>
    public uint BlockId { get; }
}
