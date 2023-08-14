// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;
using Arc.Crypto;
using CrystalData;
using CrystalData.Datum;
using LP;
using LP.Crystal;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;
using Xunit;
using xUnitTest.CrystalDataTest;

namespace xUnitTest;

public static class TestHelper
{
    public static async Task<ICrystal<TData>> CreateAndStartCrystal<TData>()
        where TData : class, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
    {
        var builder = new CrystalControl.Builder();
        builder
            .ConfigureCrystal(context =>
            {
                var directory = $"Crystal[{RandomVault.Pseudo.NextUInt32():x4}]";
                context.SetJournal(new SimpleJournalConfiguration(new LocalDirectoryConfiguration(Path.Combine(directory, "Journal"))));
                context.AddCrystal<TData>(
                    new(SavePolicy.Manual, new LocalFileConfiguration(Path.Combine(directory, "Test.tinyhand")))
                    {
                        SaveFormat = SaveFormat.Utf8,
                        NumberOfHistoryFiles = 5,
                    });
            });

        var unit = builder.Build();
        var crystalizer = unit.Context.ServiceProvider.GetRequiredService<Crystalizer>();

        var crystal = crystalizer.GetCrystal<TData>();
        var result = await crystalizer.PrepareAndLoadAll(false);
        result.Is(CrystalResult.Success);
        return crystal;
    }

    public static async Task<IBigCrystal<SimpleData>> CreateAndStartSimple(bool prepare = true)
    {
        var builder = new CrystalControl.Builder();
        builder
            .ConfigureCrystal(context =>
            {
                var directory = $"Simple[{RandomVault.Pseudo.NextUInt32():x4}]";
                context.AddBigCrystal<SimpleData>(
                    new(
                        datumRegistry =>
                        {
                            datumRegistry.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
                        },
                        SavePolicy.Manual,
                        new LocalFileConfiguration(Path.Combine(directory, "Simple")),
                        new SimpleStorageConfiguration(new LocalDirectoryConfiguration(directory))));
            });

        var unit = builder.Build();
        var crystalizer = unit.Context.ServiceProvider.GetRequiredService<Crystalizer>();

        if (prepare)
        {
            var result = await crystalizer.PrepareAndLoadAll(false);
            result.Is(CrystalResult.Success);
        }

        return crystalizer.GetBigCrystal<SimpleData>();
    }

    public static async Task<IBigCrystal<LpData>> CreateAndStartCrystal()
    {
        var builder = new CrystalControl.Builder();
        builder
            .ConfigureCrystal(context =>
            {
                var directory = $"Crystal[{RandomVault.Pseudo.NextUInt32():x4}]";
                context.AddBigCrystal<LpData>(
                    new(
                        datumRegistry =>
                        {
                            datumRegistry.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
                            datumRegistry.Register<FragmentDatum<Identifier>>(2, x => new FragmentDatumImpl<Identifier>(x));
                        },
                        SavePolicy.Manual,
                        new LocalFileConfiguration(Path.Combine(directory, "Crystal")),
                        new SimpleStorageConfiguration(new LocalDirectoryConfiguration(directory))));
            });

        var unit = builder.Build();
        var crystalizer = unit.Context.ServiceProvider.GetRequiredService<Crystalizer>();

        var crystal = crystalizer.GetBigCrystal<LpData>();
        var result = await crystal.PrepareAndLoad(false);
        result.Is(CrystalResult.Success);
        return crystal;
    }

    public static async Task UnloadAndDeleteAll(ICrystal crystal)
    {
        await crystal.Crystalizer.SaveAll(true);
        crystal.Crystalizer.Himo.MemoryUsage.Is(0);
        await crystal.Crystalizer.DeleteAll();

        if (crystal.Crystalizer.JournalConfiguration is SimpleJournalConfiguration journalConfiguration)
        {
            Directory.Delete(journalConfiguration.DirectoryConfiguration.Path, true);
        }

        var directory = Path.GetDirectoryName(crystal.CrystalConfiguration.FileConfiguration.Path);
        if (directory is not null)
        {
            Directory.EnumerateFileSystemEntries(directory).Any().IsFalse(); // Directory is empty
            Directory.Delete(directory, true);
        }
    }

    public static async Task UnloadAndDeleteAll(IBigCrystal crystal)
    {
        await crystal.Crystalizer.SaveAll(true);
        crystal.Crystalizer.Himo.MemoryUsage.Is(0);
        await crystal.Crystalizer.DeleteAll();
    }

    public static async Task StopAndStartCrystal(IBigCrystal crystal)
    {
        await crystal.Crystalizer.SaveAll(true);
        crystal.Crystalizer.Himo.MemoryUsage.Is(0);
        await crystal.PrepareAndLoad(false);
    }

    public static async Task<IBigCrystal<MergerData>> CreateAndStartMerger(int maxParent)
    {
        var builder = new CrystalControl.Builder();
        builder
            .ConfigureCrystal(context =>
            {
                var directory = $"Crystal[{RandomVault.Pseudo.NextUInt32():x4}]";
                context.AddBigCrystal<MergerData>(new BigCrystalConfiguration() with
                {
                    RegisterDatum = registry =>
                    {
                        registry.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
                    },
                    FileConfiguration = new LocalFileConfiguration(Path.Combine(directory, "Crystal")),
                    StorageConfiguration = new SimpleStorageConfiguration(new LocalDirectoryConfiguration(directory)),
                });
            })
            .SetupOptions<CrystalizerOptions>((context, options) =>
            {// CrystalizerOptions
                options.MaxParentInMemory = maxParent;
            });

        var unit = builder.Build();
        var crystalizer = unit.Context.ServiceProvider.GetRequiredService<Crystalizer>();

        var crystal = crystalizer.GetBigCrystal<MergerData>();
        var result = await crystal.PrepareAndLoad(false);
        result.Is(CrystalResult.Success);
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
