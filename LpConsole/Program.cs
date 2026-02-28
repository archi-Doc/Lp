// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using Arc;
global using Arc.Threading;
global using Arc.Unit;
global using Lp;
using Arc.Crypto;
using Lp.Data;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;

namespace LpConsole;

public class Program
{
    private static LpUnit.Product? unit;

    public static async Task Main()
    {
        AppCloseHandler.Set(() =>
        {// Console window closing or process terminated.
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
        });

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;

            try
            {
                var lpUnit = unit?.Context.ServiceProvider.GetService<LpUnit>();
                if (lpUnit != null)
                {
                    lpUnit.TryTerminate().Wait();
                }
                else
                {
                    ThreadCore.Root.Terminate(); // Send a termination signal to the root.
                }
            }
            catch
            {
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            }
        };

        var builder = new LpUnit.Builder()
            .PreConfigure(context =>
            {
            })
            .Configure(context =>
            {
                // Subcommand

                // NetService

                // ServiceFilter

                // Unit
                LpConsole.Example.ExampleUnit.Configure(context);

                // Looger resolver
                context.AddLoggerResolver(context =>
                {
                });
            });
        // .ConfigureBuilder(new LpConsole.Example.ExampleUnit.Builder()); // Alternative

        var args = SimpleParserHelper.GetCommandLineArguments();
        SimpleCommandLine.SimpleParserHelper.AddEnvironmentVariable(ref args, "lpargs");

        unit = builder.Build(args);

        var semaphoreName = $"LpConsole_{(int)XxHash3.Hash64(unit.Context.Options.DataDirectory):x8}";
        using var semaphore = new Semaphore(1, 1, semaphoreName);
        if (!semaphore.WaitOne(0))
        {
            Console.WriteLine("The application is already running, so it will be terminated.");
            ThreadCore.Root.TerminationEvent.Set();
            return;
        }

        try
        {
            var options = unit.Context.ServiceProvider.GetRequiredService<LpOptions>();
            await unit.RunAsync(options);

            await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
                                                               // unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }
        finally
        {
            semaphore.Release();
        }
    }
}
