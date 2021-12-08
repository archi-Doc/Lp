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
        blockPool = new ByteArrayPool(BlockService.MaxBlockSize);
    }

    public static ByteArrayPool.Owner Rent() => blockPool.Rent();

    private static ByteArrayPool blockPool;
}
