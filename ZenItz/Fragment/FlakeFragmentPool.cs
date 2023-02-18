// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public static class FlakeFragmentPool
{
    static FlakeFragmentPool()
    {
        pool = new ByteArrayPool(CrystalOptions.DefaultMaxDataSize, (int)(CrystalOptions.DefaultMemorySizeLimit / CrystalOptions.DefaultMaxDataSize));
        pool.SetMaxPoolBelow(CrystalOptions.DefaultMaxFragmentSize, 0);
    }

    public static ByteArrayPool Pool => pool;

    public static ByteArrayPool.Owner Rent(int minimumLength) => pool.Rent(minimumLength);

    public static void Dump(ILog logger) => pool.Dump(logger);

    private static ByteArrayPool pool;
}
