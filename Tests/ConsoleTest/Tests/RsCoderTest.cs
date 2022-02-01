// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZenItz;

namespace Test;

internal class RsCoderTest
{
    public static void SampleCode()
    {
        using (var coder = new RsCoder(8, 4))
        {// Data: 8, Check: 4 is the default (actually check > 4 does not work).
            var source = new byte[80]; // Length must be a multiple of data size.
            for (var i = 0; i < source.Length; i++)
            {
                source[i] = (byte)i;
            }

            coder.Encode(source, source.Length); // Encode to coder.EncodedBuffer[]/EncodedBufferLength.

            Console.WriteLine($"source length: {source.Length}"); // 80
            Console.WriteLine($"coder.EncodedBuffer.Length (Data+Check blocks): {coder.EncodedBuffer!.Length}"); // 12
            Console.WriteLine($"coder.EncodedBufferLength (size of block): {coder.EncodedBufferLength}"); // 10
            Console.WriteLine($"coder.EncodedBuffer[0].Length (ArrayPool allocated): {coder.EncodedBuffer![0].Length}"); // maybe 16
            Console.WriteLine();

            Console.WriteLine("Invalidate some blocks...");
            coder.EncodedBuffer![0] = null!;
            coder.EncodedBuffer![2] = null!;
            coder.EncodedBuffer![3] = null!;
            coder.EncodedBuffer![10] = null!;
            Console.WriteLine();

            coder.Decode(coder.EncodedBuffer, coder.EncodedBufferLength); // Decode to coder.DecodedBuffer/DecodedBufferLength

            Console.WriteLine($"Source and Decoded are the same: {ByteArrayEqual(source, coder.DecodedBuffer!, source.Length)}");
            Console.WriteLine();
        }
    }

    public static void SpeedTest()
    {
        const int Repeat = 10;
        var source = new byte[1024 * 1024 * 10];
        Random.Shared.NextBytes(source);

        var sw = new Stopwatch();
        var b = new RsCoder(8, 4);

        for (var n = 0; n < Repeat; n++)
        {
            sw.Restart();
            b.Encode(source, source.Length);
            sw.Stop();

            Console.WriteLine($"Encode: {10 / ((double)sw.ElapsedTicks / (double)Stopwatch.Frequency):F2} MB/sec");
        }

        for (var n = 0; n < Repeat; n++)
        {
            sw.Restart();
            b.Decode(b.EncodedBuffer!, b.EncodedBufferLength);
            sw.Stop();

            Console.WriteLine($"Decode: {10 / ((double)sw.ElapsedTicks / (double)Stopwatch.Frequency):F2} MB/sec");
        }

        Console.WriteLine();
        b.Dispose();
    }

    public static void ComprehensiveTest()
    {
        TestNM(4, 2); // Data, Check
        TestNM(4, 4);
        TestNM(8, 2);
        TestNM(8, 4); // Practical
        TestNM(16, 2);
        TestNM(16, 4); // This is the furthest I've gotten.
        // TestNM(8, 8); // Not supported
        // TestNM(16, 2);
        // TestNM(16, 4);
        // TestNM(16, 5);
        // TestNM(16, 8);

        // Encode/Decode test
        TestNM_Data(4, 2);
        TestNM_Data(4, 4);
        TestNM_Data(8, 2);
        TestNM_Data(8, 4);
        TestNM_Data(16, 2);
        TestNM_Data(16, 4);
    }

    public static void TestNM_Data(int n, int m)
    {
        Console.WriteLine($"Comprehensive RsCoder Test (Encode/Decode): {n}, check: {m}");

        var source = new byte[n];
        for (var i = 0; i < n; i++)
        {
            source[i] = (byte)i;
        }

        using (var coder = new RsCoder(n, m))
        {
            var total = 1 << coder.TotalSize;
            for (uint i = 0; i < total; i++)
            {
                if (i % 1_000_000 == 999_999)
                {
                    Console.WriteLine("1,000,000");
                }

                if (BitOperations.PopCount(i) < n)
                {// N blocks of valid data is required.
                    continue;
                }

                try
                {
                    coder.Encode(source, source.Length);
                    coder.InvalidateEncodedBufferForUnitTest(i);
                    coder.Decode(coder.EncodedBuffer!, coder.EncodedBufferLength);
                    if (ByteArrayEqual(source, coder.DecodedBuffer, source.Length) != true)
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    Console.WriteLine($"Failure: {i}");
                }
            }
        }

        Console.WriteLine("Done");
    }

    public static void TestNM(int n, int m)
    {
        Console.WriteLine($"Comprehensive RsCoder Test (Reverse matrix): {n}, check: {m}");

        using (var coder = new RsCoder(n, m))
        {
            var total = 1 << coder.TotalSize;
            for (uint i = 0; i < total; i++)
            {
                if (i % 1_000_000 == 999_999)
                {
                    Console.WriteLine("1,000,000");
                }

                if (BitOperations.PopCount(i) < n)
                {// N blocks of valid data is required.
                    continue;
                }

                try
                {
                    coder.TestReverseMatrix(i);
                }
                catch
                {
                    Console.WriteLine($"Failure: {i}");
                    // throw;
                }
            }
        }

        Console.WriteLine("Done");
    }

    private static bool ByteArrayEqual(byte[]? array1, byte[]? array2, int length)
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
}
