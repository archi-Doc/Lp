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
                coder.TestReverseMatrix(i);
            }

            /*TestNM(4, 2);
            TestNM(4, 4);
            TestNM(8, 4);
            // TestNM(8, 8);
            TestNM(16, 2);*/
            TestNM(16, 8);
        }

        public static void TestNM(int n, int m)
        {
            Console.WriteLine($"Comprehensive RsCoder Test data: {n}, check: {m}");

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
                        Console.WriteLine($"Failed: {i}");
                        // throw;
                    }
                }
            }

            Console.WriteLine("Done");
        }
    }
}
