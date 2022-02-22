// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public static class FlakeFragmentPool
{
    static FlakeFragmentPool()
    {
        pool = new ByteArrayPool(Zen.MaxFlakeSize, (int)(Zen.DefaultMemorySizeLimit / Zen.MaxFlakeSize));
        pool.SetMaxPoolBelow(Zen.MaxFragmentSize, 0);
    }

    public static ByteArrayPool Pool => pool;

    public static ByteArrayPool.Owner Rent(int minimumLength) => pool.Rent(minimumLength);

    public static void Dump(ISimpleLogger logger) => pool.Dump(logger);

    private static ByteArrayPool pool;
}
