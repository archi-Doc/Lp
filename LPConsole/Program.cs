// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Threading.Tasks;
global using Arc.Crypto;
global using Arc.Threading;
global using Arc.Unit;
global using LP;
using LP.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

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
                // context.TryParseArguments<LPOptions>();
                // context.SetData<string>("test");
                // context.TryGetData<string>(out var st);
            })
            .Configure(context =>
            {
                // Subcommand
                context.AddCommand(typeof(LPConsoleCommand));
                context.AddCommand(typeof(TempCommand));

                // NetService

                // ServiceFilter

                // Unit
                LPConsole.Sample.SampleUnit.Configure(context);

                // Looger resolver
                context.AddLoggerResolver(context =>
                {
                });
            })
            .SetupOptions<LPOptions>((context, options) =>
            {
            })
            .SetupOptions<FileLoggerOptions>((context, options) =>
            {
                options.Path = Path.Combine(context.RootDirectory, "Logs");
            });
        // .ConfigureBuilder(new LPConsole.Sample.SampleUnit.Builder()); // Alternative

        unit = builder.Build();

        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.Context.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = true,
        };

        await SimpleParser.ParseAndRunAsync(unit.Context.Commands, args, parserOptions); // Main process
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        // unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }

    private static Control.Unit? unit;
}
