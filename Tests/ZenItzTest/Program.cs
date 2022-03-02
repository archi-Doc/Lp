// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using System;
global using System.Threading;
global using System.Threading.Tasks;
global using Arc.Threading;
global using CrossChannel;
global using LP;
global using ZenItz;
using DryIoc;
using Serilog;
using SimpleCommandLine;
using Tinyhand;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ZenItzTest;

public class Program
{
    public static Container Container { get; } = new();

    public static async Task Main(string[] args)
    {
        // Subcommands
        var commandTypes = new List<Type>();
        commandTypes.Add(typeof(ZenTestSubcommand));
        commandTypes.Add(typeof(ItzTestSubcommand));

        // DI Container
        ZenControl.Register(Container, commandTypes);
        foreach (var x in commandTypes)
        {
            Container.Register(x, Reuse.Singleton);
        }

        // Services

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

        // Logger
        /*if (options.EnableLogger)
        {
            var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
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
                .CreateLogger()));
        }*/

        Container.Resolve<ZenControl>().Zen.SetDelegate(ObjectToMemoryOwner, MemoryOwnerToObject);

        await SimpleParser.ParseAndRunAsync(commandTypes, args, parserOptions); // Main process

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        Logger.CloseAndFlush();
        // await Task.Delay(1000);
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }

    public static bool ObjectToMemoryOwner(object? obj, out ByteArrayPool.MemoryOwner dataToBeMoved)
    {
        if (obj is LP.Fragments.FragmentBase flake &&
            FlakeFragmentService.TrySerialize<LP.Fragments.FragmentBase>(flake, out dataToBeMoved))
        {
            return true;
        }
        else
        {
            dataToBeMoved = default;
            return false;
        }
    }

    public static object? MemoryOwnerToObject(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
        if (TinyhandSerializer.TryDeserialize<LP.Fragments.FragmentBase>(memoryOwner.Memory, out var value))
        {
            return value;
        }
        else
        {
            return null;
        }
    }
}
