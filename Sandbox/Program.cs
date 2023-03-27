// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc.Threading;
global using CrystalData;
global using Tinyhand;
global using LP;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                context.AddSingleton<TestClass>();
                context.Services.Add(ServiceDescriptor.Singleton(typeof(ManualClass), provider => provider.GetRequiredService<ICrystal<ManualClass>>().Object));
            })
            .Crystalize(context =>
            {
                context.TryAdd<ManualClass>(Crystalization.None);
            });

        var unit = builder.Build();

        var tc = unit.Context.ServiceProvider.GetRequiredService<TestClass>();
        await tc.Test1();

        /*var param = new CrystalControl.Unit.Param();

        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.Context.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = true,
        };

        await SimpleParser.ParseAndRunAsync(unit.Context.Commands, args, parserOptions); // Main process*/

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
