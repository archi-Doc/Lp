// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Unit;
using CrystalData;
using LP;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public static class TestHelper
{
    public static async Task<LpCrystal> CreateAndStartZen()
    {
        var options = new CrystalOptions() with
        {
            CrystalPath = $"Zen[{LP.Random.Pseudo.NextUInt32():x4}]",
            DefaultZenDirectory = "Snowflake",
        };

        var unit = new CrystalControl.Builder().Build();
        var crystal = unit.Context.ServiceProvider.GetRequiredService<LpCrystal>();
        await crystal.StartAsync(new(FromScratch: true));
        return crystal;
    }

    public static async Task StopZen(LpCrystal zen, bool removeAll = true)
    {
        await zen.StopAsync(new(RemoveAll: removeAll));
        zen.MemoryUsage.Is(0);
    }

    public static async Task StopAndStartZen(LpCrystal zen)
    {
        await zen.StopAsync(new());
        zen.MemoryUsage.Is(0);
        await zen.StartAsync(new());
    }

    public static bool DataEquals(this CrystalMemoryResult dataResult, Span<byte> span)
    {
        return dataResult.Data.Span.SequenceEqual(span);
    }

    public static bool ByteArrayEquals(byte[]? array1, byte[]? array2, int length)
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
