// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Blocks;

internal static class BlockPool
{
    static BlockPool()
    {
        blockPool = new FixedArrayPool(BlockService.MaxBlockSize);
    }

    public static FixedArrayPool.Owner Rent() => blockPool.Rent();

    private static FixedArrayPool blockPool;
}
