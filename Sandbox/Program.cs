// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc.Threading;
global using CrystalData;
global using Tinyhand;
global using LP;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using CrystalData.Datum;
using Tinyhand.IO;
using CrystalData.Storage;
using SimpleCommandLine;

namespace Sandbox;

public class Program
{
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {// Console window closing or process terminated.
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
        };

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        };

        var builder = new CrystalControl.Builder()
            .Preload(context =>
            {
                context.DataDirectory = "Data";
            })
            .Configure(context =>
            {
                context.AddSingleton<TestClass>();
            })
            .ConfigureCrystal(context =>
            {
                context.SetJournal(new SimpleJournalConfiguration(new LocalDirectoryConfiguration("Journal")));

                context.AddCrystal<ManualClass>(
                    new(
                        Crystalization.Manual,
                        new LocalFileConfiguration("manual.data")
                        ));

                context.AddCrystal<CombinedClass>(
                    new(
                        Crystalization.Periodic,
                        new LocalFileConfiguration("combined.data"),
                        new SimpleStorageConfiguration(new LocalDirectoryConfiguration("simple"))));

                context.AddBigCrystal<BaseData>(new BigCrystalConfiguration() with
                {
                    RegisterDatum = registry =>
                    {
                        registry.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
                    },
                    DirectoryConfiguration = new LocalDirectoryConfiguration("Crystal"),
                    StorageConfiguration = new SimpleStorageConfiguration(new LocalDirectoryConfiguration("Storage")),
                });

                context.AddBigCrystal<ExampleData>(new BigCrystalConfiguration() with
                {
                    RegisterDatum = registry =>
                    {
                        registry.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
                    },
                    DirectoryConfiguration = new LocalDirectoryConfiguration("Example"),
                    StorageConfiguration = new SimpleStorageConfiguration(new LocalDirectoryConfiguration("Example")),
                });
            })
            .SetupOptions<CrystalizerOptions>((context, options) =>
            {// CrystalizerOptions
                options.EnableLogger = true;
                options.RootPath = Directory.GetCurrentDirectory();
            });

        var unit = builder.Build();

        if (SimpleParserHelper.TryGetAndRemoveArgument(ref args, "storagekey", out var bucketKeyPair))
        {
            if (AccessKeyPair.TryParse(bucketKeyPair, out var bucket, out var accessKeyPair))
            {
                unit.Context.ServiceProvider.GetRequiredService<IStorageKey>().AddKey(bucket, accessKeyPair);
            }
        }

        var tc = unit.Context.ServiceProvider.GetRequiredService<TestClass>();
        await tc.Test1();

        ThreadCore.Root.Terminate();
        await unit.Context.ServiceProvider.GetRequiredService<Crystalizer>().SaveAllAndTerminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        if (unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate() is { } task)
        {
            await task;
        }

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
