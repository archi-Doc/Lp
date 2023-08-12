// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc.Threading;
global using CrystalData;
global using Tinyhand;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using CrystalData.Datum;
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
            .Configure(context =>
            {
                context.AddSingleton<TestClass0>();
                context.AddSingleton<TestClass>();
                context.AddLoggerResolver(context =>
                {
                    /*if (context.LogLevel == LogLevel.Debug)
                    {
                        context.SetOutput<FileLogger<FileLoggerOptions>>();
                        return;
                    }*/

                    context.SetOutput<ConsoleAndFileLogger>();
                });
            })
            .ConfigureCrystal(context =>
            {
                context.SetJournal(
                    new SimpleJournalConfiguration(new LocalDirectoryConfiguration("Journal"))
                    {
                        // BackupDirectoryConfiguration = new LocalDirectoryConfiguration("Backup/Journal"),
                    });

                context.AddCrystal<ManualClass>(
                    new(SavePolicy.OnChanged, new RelativeFileConfiguration("Local/manual.tinyhand"))
                    {
                        SaveFormat = SaveFormat.Utf8,
                        NumberOfHistoryFiles = 3,
                        // BackupFileConfiguration = new LocalFileConfiguration("Backup/manual.tinyhand")
                    });

                /*context.AddCrystal<CombinedClass>(
                    new(
                        SavePolicy.Periodic,
                        new LocalFileConfiguration("Local/combined"),
                        new SimpleStorageConfiguration(new LocalDirectoryConfiguration("Local/Simple"), new LocalDirectoryConfiguration("Backup/Simple")))
                    {
                        SaveInterval = TimeSpan.FromSeconds(10),
                        // BackupFileConfiguration = new LocalFileConfiguration("Backup/combined")
                    });*/

                /*context.AddCrystal<ValueClass.GoshujinClass>(
                    CrystalConfiguration.SingleUtf8(false, new LocalFileConfiguration("Local/ValueClass.tinyhand")));

                context.AddCrystal<StandardData.GoshujinClass>(
                    CrystalConfiguration.SingleUtf8(false, new LocalFileConfiguration("Local/StandardData.tinyhand")));

                context.AddBigCrystal<BaseData>(new BigCrystalConfiguration() with
                {
                    RegisterDatum = registry =>
                    {
                        registry.Register<ObjectDatum<LocalFileConfiguration>>(1, x => new ObjectDatumImpl<LocalFileConfiguration>(x));
                    },
                    FileConfiguration = new LocalFileConfiguration("Local/BaseData/Crystal"),
                    // BackupFileConfiguration = new LocalFileConfiguration("Backup/BaseData/Crystal"),
                    StorageConfiguration = new SimpleStorageConfiguration(new LocalDirectoryConfiguration("Local/BaseData/Storage")),
                });*/

                /*context.AddBigCrystal<ExampleData>(new BigCrystalConfiguration() with
                {
                    RegisterDatum = registry =>
                    {
                        registry.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
                    },
                    DirectoryConfiguration = new LocalDirectoryConfiguration("Example"),
                    StorageConfiguration = new SimpleStorageConfiguration(new LocalDirectoryConfiguration("Example")),
                });*/
            })
            .SetupOptions<CrystalizerOptions>((context, options) =>
            {// CrystalizerOptions
                options.EnableFilerLogger = true;
                options.RootPath = Directory.GetCurrentDirectory();
                options.GlobalMain = new LocalDirectoryConfiguration("Relative");
                // options.GlobalBackup = new LocalDirectoryConfiguration("Backup2");
            })
            .SetupOptions<FileLoggerOptions>((context, options) =>
            {// FileLoggerOptions
                var logfile = "Logs/Log.txt";
                options.Path = Path.Combine(context.RootDirectory, logfile);
                options.MaxLogCapacity = 2;
            });

        var unit = builder.Build();

        if (SimpleParserHelper.TryGetAndRemoveArgument(ref args, "storagekey", out var bucketKeyPair))
        {
            if (AccessKeyPair.TryParse(bucketKeyPair, out var bucket, out var accessKeyPair))
            {
                unit.Context.ServiceProvider.GetRequiredService<IStorageKey>().AddKey(bucket, accessKeyPair);
            }
        }

        // var sc = new SimpleJournalConfiguration(new LocalDirectoryConfiguration("Storage"));
        // var st = TinyhandSerializer.SerializeToString((JournalConfiguration)sc);

        var tc = unit.Context.ServiceProvider.GetRequiredService<TestClass0>();
        await tc.Test1();

        ThreadCore.Root.Terminate();
        await unit.Context.ServiceProvider.GetRequiredService<Crystalizer>().SaveAllAndTerminate();
        // await unit.Context.ServiceProvider.GetRequiredService<Crystalizer>().SaveJournalOnlyForTest();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        if (unit.Context.ServiceProvider.GetService<UnitLogger>() is { } unitLogger)
        {
            await unitLogger.FlushAndTerminate();
        }

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
