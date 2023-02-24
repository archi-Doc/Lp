// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public static class BlockPool
{
    public const int MaxPool = 100;

    public const int MaxBlockSize = 1024 * 1024 * 4; // 4MB
    public const int StandardBlockSize = 32 * 1024; // 32KB
    public const int StandardBlockPool = 500;

    static BlockPool()
    {
        pool = new ByteArrayPool(MaxBlockSize, MaxPool);
        pool.SetMaxPool(StandardBlockSize, StandardBlockPool);
    }

    public static ByteArrayPool.Owner Rent(int minimumLength) => pool.Rent(minimumLength);

    public static void Dump(ILog logger) => pool.Dump(logger);

    private static ByteArrayPool pool;
}
