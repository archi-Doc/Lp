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
            if (unit?.Context.ServiceProvider.GetService<ExecutionStack>() is { } executionStack)
            {
                // executionStack.TopContext?.Signal(ExecutionSignal.Exit);
                while (executionStack.FirstCore is { } core)
                {
                    core.Dispose();
                    Thread.Sleep(100);
                }
            }

            var result = unit?.Context.Root.WaitForTermination(TimeSpan.FromSeconds(2)).Result;
            if (result != true)
            {
                unit?.Context.Root.RequestTermination(); // Send a termination signal to the root.
                unit?.Context.Root.WaitForTermination(TimeSpan.FromSeconds(2)).Wait();
            }
        });

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;

            if (unit?.Context.ServiceProvider.GetService<LpUnit>()?.ExecutionStack is { } executionStack)
            {
                if (executionStack.IsEmpty)
                {
                    try
                    {
                        var lpUnit = unit?.Context.ServiceProvider.GetService<LpUnit>();
                        if (lpUnit != null)
                        {
                            lpUnit.TryTerminate().Wait();
                        }
                        else
                        {
                            unit?.Context.Root.RequestTermination(); // Send a termination signal to the root.
                        }
                    }
                    catch
                    {
                        unit?.Context.Root.RequestTermination(); // Send a termination signal to the root.
                    }
                }
                else
                {
                    executionStack.LastCore?.SendSignal(ExecutionSignal.Exit);
                }
            }

            // var keyInfo = new ConsoleKeyInfo(keyChar: '\u0003', ConsoleKey.C, false, false, true);
            // SimpleConsole.GetOrCreate().EnqueueKey(keyInfo);
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
                    if (context.LogLevel == LogLevel.Debug)
                    {//
                        context.SetOutput<ConsoleLogger>();
                        return;
                    }
                });
            });
        // .ConfigureBuilder(new LpConsole.Example.ExampleUnit.Builder()); // Alternative

        var args = SimpleParserHelper.GetCommandLineArguments();
        SimpleCommandLine.SimpleParserHelper.AppendEnvironmentVariable(ref args, "lpargs");

        unit = builder.Build(args);

        var semaphoreName = OperatingSystem.IsWindows() ? $"LpConsole_{(int)XxHash3.Hash64(unit.Context.Options.DataDirectory):x8}" : default; // Named semaphores are not supported on Linux.
        using var semaphore = new Semaphore(1, 1, semaphoreName);
        if (!semaphore.WaitOne(0))
        {
            Console.WriteLine("The application is already running, so it will be terminated.");
            return;
        }

        try
        {
            var options = unit.Context.ServiceProvider.GetRequiredService<LpOptions>();
            await unit.Run(options);
            await unit.Context.Root.WaitForTermination(TerminationOptions.IncludeIndependent); // Wait for the termination infinitely.
        }
        finally
        {
            semaphore.Release();
        }
    }
}
