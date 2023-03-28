// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using Arc.Unit;
using CrystalData;
using CrystalData.Datum;
using LP;
using LP.Crystal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace xUnitTest;

public static class TestHelper
{
    public static async Task<LpCrystal> CreateAndStartCrystal()
    {
        var options = new CrystalOptions() with
        {
            CrystalPath = $"Crystal[{RandomVault.Pseudo.NextUInt32():x4}]",
            DefaultCrystalDirectory = "Snowflake",
        };

        var builder = new CrystalControl.Builder();
        builder
            .Configure(context =>
            {
                context.AddSingleton<LpCrystal>();
                context.Services.Add(ServiceDescriptor.Transient(typeof(LpData), x => x.GetRequiredService<LpCrystal>().Root.Data));
            })
            .SetupOptions<CrystalOptions>((context, options) =>
            {
                options.CrystalPath = $"Crystal[{RandomVault.Pseudo.NextUInt32():x4}]";
            });

        var unit = builder.Build();
        var crystal = unit.Context.ServiceProvider.GetRequiredService<LpCrystal>();
        crystal.Datum.Register<FragmentDatum<Identifier>>(2, x => new FragmentDatumImpl<Identifier>(x));
        await crystal.StartAsync(new(FromScratch: true));
        return crystal;
    }

    public static async Task StopCrystal(ICrystalData crystal, bool removeAll = true)
    {
        await crystal.StopAsync(new(RemoveAll: removeAll));
        crystal.MemoryUsage.Is(0);
    }

    public static async Task StopAndStartCrystal(ICrystalData crystal)
    {
        await crystal.StopAsync(new());
        crystal.MemoryUsage.Is(0);
        await crystal.StartAsync(new());
    }

    public static async Task<MergerCrystal> CreateAndStartMerger(int maxParent)
    {
        var builder = new CrystalControl.Builder();
        builder
            .Configure(context =>
            {
                context.AddSingleton<MergerCrystal>();
                context.Services.Add(ServiceDescriptor.Transient(typeof(LpData), x => x.GetRequiredService<MergerCrystal>().Root));
            })
            .SetupOptions<CrystalOptions>((context, options) =>
            {
                options.CrystalPath = $"Crystal[{RandomVault.Pseudo.NextUInt32():x4}]";
                options.MaxParentInMemory = maxParent;
            });

        var unit = builder.Build();
        var crystal = unit.Context.ServiceProvider.GetRequiredService<MergerCrystal>();
        await crystal.StartAsync(new(FromScratch: true));
        return crystal;
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
