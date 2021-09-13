// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using LP.Zen;

namespace Test;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Manual test");
        Console.WriteLine();

        RsCoderTest.SampleCode();
        RsCoderTest.ComprehensiveTest();
    }
}
