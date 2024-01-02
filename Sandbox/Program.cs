// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc.Threading;
global using CrystalData;
global using Tinyhand;
using System.Runtime.InteropServices;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using SimpleCommandLine;

namespace Sandbox;

public class TestServerContext : ServerContext
{
    public TestServerContext()
    {
    }
}

public class TestCallContext : CallContext<TestServerContext>
{
    public static new TestCallContext Current => (TestCallContext)CallContext.Current;

    public TestCallContext()
    {
    }
}

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

        var builder = new NetControl.Builder()
            .Configure(context =>
            {
                // Command
                context.AddCommand(typeof(SandboxSubcommand));
                context.AddCommand(typeof(BasicTestSubcommand));
                context.AddCommand(typeof(BlockTestSubcommand));

                context.AddLoggerResolver(context =>
                {
                    if (context.LogLevel == LogLevel.Debug)
                    {
                        // context.SetOutput<FileLogger<FileLoggerOptions>>();
                        // return;
                    }

                    context.SetOutput<ConsoleAndFileLogger>();
                });
            })
            .SetupOptions<FileLoggerOptions>((context, options) =>
            {// FileLoggerOptions
                var logfile = "Logs/Debug.txt";
                options.Path = Path.Combine(context.RootDirectory, logfile);
                options.MaxLogCapacity = 1;
                options.Formatter.TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff K";
                options.ClearLogsAtStartup = true;
            })
            .SetupOptions<NetsphereOptions>((context, options) =>
            {// NetsphereOptions
                options.EnableEssential = true;
                options.EnableAlternative = true;
            });

        var unit = builder.Build();
        var options = unit.Context.ServiceProvider.GetRequiredService<NetsphereOptions>();
        await Console.Out.WriteLineAsync($"Port: {options.Port.ToString()}");
        var param = new NetControl.Unit.Param(true, () => new TestServerContext(), () => new TestCallContext(), "test", options, true);
        await unit.RunStandalone(param);

        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.Context.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = false,
        };

        await SimpleParser.ParseAndRunAsync(unit.Context.Commands, args, parserOptions); // Main process

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        if (unit.Context.ServiceProvider.GetService<UnitLogger>() is { } unitLogger)
        {
            await unitLogger.FlushAndTerminate();
        }

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
