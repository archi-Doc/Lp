// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;

namespace CrystalData;

internal static class Random
{
    public const int VaultSize = 1024;

    static Random()
    {
        var xo = new Xoshiro256StarStar();
        Pseudo = new RandomVault(() => xo.NextUInt64(), x => xo.NextBytes(x), VaultSize);
    }

    /// <summary>
    ///  Gets pseudo random number pool.
    /// </summary>
    public static RandomVault Pseudo { get; }
}
