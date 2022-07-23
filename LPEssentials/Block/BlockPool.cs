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
        pool = new ByteArrayPool(BlockService.MaxBlockSize, MaxPool);
        pool.SetMaxPool(BlockService.StandardBlockSize, BlockService.StandardBlockPool);
    }

    public static ByteArrayPool.Owner Rent(int minimumLength) => pool.Rent(minimumLength);

    public static void Dump(ILogger logger) => pool.Dump(logger);

    private static ByteArrayPool pool;
}
