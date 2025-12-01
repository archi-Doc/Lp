// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc;
global using Arc.Threading;
global using Arc.Unit;
global using Lp;

using Lp.Data;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;

namespace LpConsole;

public class Program
{
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

            var lpUnit = unit?.Context.ServiceProvider.GetService<LpUnit>();
            if (lpUnit != null)
            {
                lpUnit.TryTerminate().Wait();
            }
            else
            {
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            }
        };

        await Task.Delay(10000);//
        Console.WriteLine($"{Console.CursorLeft}, {Console.CursorTop}");

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

        var options = unit.Context.ServiceProvider.GetRequiredService<LpOptions>();
        await unit.RunAsync(options);

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        // unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }

    private static LpUnit.Product? unit;
}
