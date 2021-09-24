// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class Random
{
    public const int VaultSize = 1024;

    static Random()
    {
        var xo = new Xoshiro256StarStar();
        Pseudo = new RandomVault(() => xo.NextULong(), x => xo.NextBytes(x), VaultSize);
        Crypto = new RandomVault(null, x => RandomNumberGenerator.Fill(x), VaultSize);
    }

    public static RandomVault Crypto { get; }

    public static RandomVault Pseudo { get; }
}
