// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public static class FlakeFragmentPool
{
    static FlakeFragmentPool()
    {
        pool = new ByteArrayPool(ZenOptions.DefaultMaxDataSize, (int)(ZenOptions.DefaultMemorySizeLimit / ZenOptions.DefaultMaxDataSize));
        pool.SetMaxPoolBelow(ZenOptions.DefaultMaxFragmentSize, 0);
    }

    public static ByteArrayPool Pool => pool;

    public static ByteArrayPool.Owner Rent(int minimumLength) => pool.Rent(minimumLength);

    public static void Dump(ILog logger) => pool.Dump(logger);

    private static ByteArrayPool pool;
}
