// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using LP;
using Microsoft.Extensions.DependencyInjection;
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
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        };

        var builder = new Control.Builder()
            .Configure(context =>
            {
                // Subcommand
                context.AddCommand(typeof(LPConsoleCommand));
                context.AddCommand(typeof(CreateKeyCommand));

                // NetService

                // ServiceFilter

                // Unit
                context.AddSingletonUnit<LP.Custom.CustomUnit>();
            });
            // .ConfigureBuilder(new LP.Custom.CustomUnit.Builder());

        var unit = builder.Build();

        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = true,
        };

        await SimpleParser.ParseAndRunAsync(unit.CommandTypes, args, parserOptions); // Main process
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).

        Logger.CloseAndFlush();
    }
}
