// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc.Threading;
global using CrystalData;
global using LP;
using Arc.Unit;
using CrystalData.Datum;
using LP.Crystal;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;

namespace CrystalDataTest;

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
                // Command
                context.AddCommand(typeof(CrystalTestSubcommand));
                context.AddCommand(typeof(MonoTestSubcommand));

                // Services
            })
            .ConfigureCrystal(context =>
            {
                context.AddBigCrystal<LpData>(new BigCrystalConfiguration() with
                {// LpData
                    RegisterDatum = registry =>
                    {
                        registry.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
                        registry.Register<FragmentDatum<Identifier>>(2, x => new FragmentDatumImpl<Identifier>(x));
                    },
                    FileConfiguration = new LocalFileConfiguration("Data/Crystal"),
                    StorageConfiguration = new SimpleStorageConfiguration(new LocalDirectoryConfiguration("Crystal")),
                });
            });

        var unit = builder.Build();

        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.Context.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = true,
        };

        await SimpleParser.ParseAndRunAsync(unit.Context.Commands, args, parserOptions); // Main process

        ThreadCore.Root.Terminate();
        await unit.Context.ServiceProvider.GetRequiredService<Crystalizer>().SaveAllAndTerminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
