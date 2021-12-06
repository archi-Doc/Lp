// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP;

internal static class BlockPool
{
    public const int MaxBlockSize = 1024 * 1024 * 4; // 4MB

    static BlockPool()
    {
        blockPool = new ByteArrayPool(MaxBlockSize);
    }

    public static ByteArrayPool.Owner Rent() => blockPool.Rent();

    private static ByteArrayPool blockPool;
}
