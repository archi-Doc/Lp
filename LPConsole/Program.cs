// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using DryIoc;
using LP;
using Serilog;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace LPConsole
{
    public class Program
    {
        public static Container Container { get; } = new();

        public static async Task Main(string[] args)
        {
            // Simple Commands
            var commandTypes = new Type[]
            {
                typeof(LPConsoleCommand),
                typeof(TestCommand2),
            };

            // DI Container
            LPCore.Register(Container);
            foreach (var x in commandTypes)
            {
                Container.Register(x, Reuse.Singleton);
            }

            Container.ValidateAndThrow();

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

            var parserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = Container,
                RequireStrictCommandName = false,
                RequireStrictOptionName = true
            };

            await SimpleParser.ParseAndRunAsync(commandTypes, args, parserOptions); // Main process
            await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }
    }
}
