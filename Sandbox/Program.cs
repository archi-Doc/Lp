﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using LP.Zen;

namespace Sandbox;

internal class Program
{
    public static bool IsAlmostEqual(byte[]? array1, byte[]? array2, int length)
    {
        if (array1 == null || array2 == null)
        {
            return false;
        }
        else if (array1.Length < length || array2.Length < length)
        {
            return false;
        }

        for (var n = 0; n < length; n++)
        {
            if (array1[n] != array2[n])
            {
                return false;
            }
        }

        return true;
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("ConsoleApp1");

        var source = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, };
        byte[][]? destination = null;

        RsCoder b;
        using (b = new RsCoder(8, 8, 285))
        {
            b.Encode(source, source.Length);

            b.Decode(b.EncodedBuffer!, b.EncodedBufferLength);
            var comp = IsAlmostEqual(source, b.DecodedBuffer, source.Length);

            // b.InvalidateEncodedBufferForUnitTest(new Random(3), 2);
            b.Decode(b.EncodedBuffer!, b.EncodedBufferLength);

            destination = b.EncodedBuffer!;
            destination[0] = null!;
            destination[1] = null!;
            destination[2] = null!;
            destination[6] = null!;
            destination[10] = null!;
            destination[14] = null!;
            destination[15] = null!;
            b.Decode(b.EncodedBuffer!, b.EncodedBufferLength);

            destination = b.EncodedBuffer!;
            b.InvalidateEncodedBufferForUnitTest(new Random(3), 8);
            b.Decode(destination, b.EncodedBufferLength);
            comp = IsAlmostEqual(source, b.DecodedBuffer, source.Length);
        }

        source = new byte[1024 * 1024 * 10];
        Random.Shared.NextBytes(source);

        b = new RsCoder(16, 16);
        b = new RsCoder(16, 16);
        var sw = new Stopwatch();

        sw.Restart();
        b.Encode(source, source.Length);
        sw.Stop();

        Console.WriteLine($"{10 / ((double)sw.ElapsedMilliseconds / 1000):F2} MB/sec");

        sw.Restart();
        b.Decode(b.EncodedBuffer!, b.EncodedBufferLength);
        sw.Stop();

        b.Dispose();
        Console.WriteLine($"{10 / ((double)sw.ElapsedMilliseconds / 1000):F2} MB/sec");
    }
}
