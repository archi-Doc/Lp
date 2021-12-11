// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Block;

public static class BlockPool
{
    public const int MaxPool = 100;
    

    static BlockPool()
    {
        blockPool = new ByteArrayPool(BlockService.MaxBlockSize, MaxPool);
        blockPool.SetMaxPool(BlockService.StandardBlockSize, BlockService.StandardBlockPool);
    }

    public static ByteArrayPool.Owner Rent(int minimumLength) => blockPool.Rent(minimumLength);

    private static ByteArrayPool blockPool;
}
