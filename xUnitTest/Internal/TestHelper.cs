// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Unit;
using LP;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;
using Xunit;
using ZenItz;

namespace xUnitTest;

public static class TestHelper
{
    public static async Task<Zen<TIdentifier>> CreateAndStartZen<TIdentifier>()
        where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
    {
        var options = new ZenOptions() with
        {
            ZenPath = $"Zen[{LP.Random.Pseudo.NextUInt32():x4}]",
            DefaultZenDirectory = "Snowflake",
        };

        var unit = new ZenControl.Builder().Build();
        var zenControl = unit.Context.ServiceProvider.GetRequiredService<ZenControl>();
        var zen = zenControl.CreateZen<TIdentifier>(options);
        await zen.StartAsync(new(FromScratch: true));
        return zen;
    }

    public static async Task StopZen<TIdentifier>(Zen<TIdentifier> zen, bool removeAll = true)
        where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
    {
        await zen.StopAsync(new(RemoveAll: removeAll));
        zen.MemoryUsage.Is(0);
    }

    public static async Task StopAndStartZen<TIdentifier>(Zen<TIdentifier> zen)
        where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
    {
        await zen.StopAsync(new());
        zen.MemoryUsage.Is(0);
        await zen.StartAsync(new());
    }

    public static bool DataEquals(this ZenMemoryResult dataResult, Span<byte> span)
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
