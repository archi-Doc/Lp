// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using System;
global using System.Threading;
global using System.Threading.Tasks;
global using Arc.Threading;
global using CrossChannel;
global using LP;
global using Netsphere;
using DryIoc;
using Serilog;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace NetsphereTest;

public class Program
{
    public static Container Container { get; } = new();

    public static async Task Main(string[] args)
    {
        // Subcommands
        var commandTypes = new List<Type>();
        commandTypes.Add(typeof(BasicTestSubcommand));
        commandTypes.Add(typeof(NetbenchSubcommand));

        // DI Container
        NetControl.Register(Container, commandTypes);
        foreach (var x in commandTypes)
        {
            Container.Register(x, Reuse.Singleton);
        }

        Container.Register<ExternalServiceImpl>(Reuse.Singleton);

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
            RequireStrictOptionName = true,
        };

        var options = new LP.Options.NetsphereOptions();
        options.EnableAlternative = true;
        options.EnableTest = true;
        NetControl.QuickStart("test", options, true);

        // Logger
        /*var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(logDirectory);
        var netControl = Container.Resolve<NetControl>();
        netControl.Terminal.SetLogger(new SerilogLogger(new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "terminal.log.txt"),
                buffered: true,
                flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
            .CreateLogger()));
        netControl.Alternative?.SetLogger(new SerilogLogger(new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "terminal2.log.txt"),
                buffered: true,
                flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
            .CreateLogger()));*/

        // await SimpleParser.ParseAndRunAsync(commandTypes, "netbench -node alternative", parserOptions); // Main process
        await SimpleParser.ParseAndRunAsync(commandTypes, args, parserOptions); // Main process

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        Logger.CloseAndFlush();
        await Task.Delay(1000);
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
