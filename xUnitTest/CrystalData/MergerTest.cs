// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;
using Tinyhand;
using Xunit;

namespace xUnitTest.CrystalDataTest;

public partial class MergerTest
{
    public const int N = 20;
    public const int MaxParent = 10;

    public MergerTest()
    {
    }

    [Fact]
    public async Task Test1()
    {
        var crystal = await TestHelper.CreateAndStartMerger(MaxParent);
        var root = crystal.Data;
        var byteArray = new byte[] { 0, 1, 2, };
        await TestHelper.StopAndStartCrystal(crystal);
        root = crystal.Data;

        for (var i = 0; i < N; i++)
        {
            var d = root.GetOrCreateChild(new(i));
            var d2 = d.GetOrCreateChild(new(0)); // Parent data
            d.BlockDatum().Set(byteArray);
        }

        crystal.Crystalizer.Himo.Unload();

        var count = 0;
        for (var i = 0; i < N; i++)
        {
            var d = root.GetOrCreateChild(new(i));
            if (d.IsInMemory)
            {
                count++;
            }
        }

        (count <= MaxParent).IsTrue();

        await TestHelper.StopAndStartCrystal(crystal);

        count = 0;
        for (var i = 0; i < N; i++)
        {
            var d = root.GetOrCreateChild(new(i));
            if (d.IsInMemory)
            {
                count++;
            }
        }

        count.Is(0);

        for (var i = 0; i < N; i++)
        {
            var d = root.TryGetChild(new(i));
            d.IsNotNull();

            var result = await d!.BlockDatum().Get();
            result.Data.Span.SequenceEqual(byteArray).IsTrue();
        }

        await TestHelper.UnloadAndDeleteAll(crystal);
    }
}
