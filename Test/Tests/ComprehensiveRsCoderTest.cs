// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LP.Zen;

namespace Test
{
    internal class ComprehensiveRsCoderTest
    {
        public static void Test()
        {
            using (var coder = new RsCoder(16, 8))
            {
                uint i = 1268735;
                // coder.TestReverseMatrix(i);
            }

            TestNM(4, 2);
            TestNM(4, 4);
            TestNM(8, 4);
            // TestNM(8, 8);
            // TestNM(16, 2);
            // TestNM(16, 4);
            // TestNM(16, 5);
            // TestNM(16, 8);

            TestNM_Data(4, 2);
            TestNM_Data(4, 4);
            TestNM_Data(8, 4);
            TestNM_Data(8, 5);
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
                    {
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
                    {
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
}
