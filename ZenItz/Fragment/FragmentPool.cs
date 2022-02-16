// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Fragment;

public static class FragmentPool
{
    static FragmentPool()
    {
        pool = new ByteArrayPool(FragmentService.MaxFragmentSize, 0);
    }

    public static ByteArrayPool.Owner Rent(int minimumLength) => pool.Rent(minimumLength);

    public static void Dump(ISimpleLogger logger) => pool.Dump(logger);

    private static ByteArrayPool pool;
}
