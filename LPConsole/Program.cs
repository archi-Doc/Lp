// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Threading.Tasks;
global using Arc.Threading;
global using Arc.Unit;
global using LP;
using LP.Data;
using Microsoft.Extensions.DependencyInjection;

namespace LPConsole;

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

            var control = unit?.Context.ServiceProvider.GetService<Control>();
            if (control != null)
            {
                control.TryTerminate().Wait();
            }
            else
            {
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            }
        };

        var builder = new Control.Builder()
            .Preload(context =>
            {
            })
            .Configure(context =>
            {
                // Subcommand

                // NetService

                // ServiceFilter

                // Unit
                LPConsole.Example.ExampleUnit.Configure(context);

                // Looger resolver
                context.AddLoggerResolver(context =>
                {
                });
            });
        // .ConfigureBuilder(new LPConsole.Example.ExampleUnit.Builder()); // Alternative

        SimpleCommandLine.SimpleParserHelper.AddEnvironmentVariable(ref args, "lpargs");

        unit = builder.Build(args);

        var options = unit.Context.ServiceProvider.GetRequiredService<LPOptions>();
        await unit.RunAsync(options);

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        // unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }

    private static Control.Unit? unit;
}
