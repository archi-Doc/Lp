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
using SimplePrompt;

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

        var semaphoreName = OperatingSystem.IsWindows() ? $"LpConsole_{(int)XxHash3.Hash64(unit.Context.Options.DataDirectory):x8}" : default; // Named semaphores are not supported on Linux.
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
            await unit.Run(options);

            await ThreadCore.Root.WaitForTermination(); // Wait for the termination infinitely.
                                                               // unit.Context.ServiceProvider.GetService<LogUnit>()?.FlushAndTerminate();
            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }
        finally
        {
            semaphore.Release();
        }
    }
}
