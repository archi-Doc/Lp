// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class Random
{
    static Random()
    {
        Span<byte> b = stackalloc byte[1024];
        RandomNumberGenerator.Fill(b);
        mt = new MersenneTwister();
    }

    public static long NextLong()
    {
        lock (mt)
        {
            return mt.NextLong();
        }
    }

    private static MersenneTwister mt;
}
