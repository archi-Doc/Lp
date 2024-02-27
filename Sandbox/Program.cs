// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc.Threading;
global using CrystalData;
global using Tinyhand;
using Arc.Crypto;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using Netsphere.Crypto;
using Netsphere.Logging;
using SimpleCommandLine;

namespace Sandbox;

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
                {// Logger
                    if (context.LogLevel == LogLevel.Debug)
                    {
                        context.SetOutput<FileLogger<FileLoggerOptions>>();
                        return;
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
                options.MaxQueue = 100_000;
            })
            .SetupOptions<NetOptions>((context, options) =>
            {// NetsphereOptions
                options.NodeName = "test";
                options.EnableEssential = true;
                options.EnableServer = true;
                options.EnableAlternative = true;
            });

        // Netsphere
        var unit = builder.Build();
        var options = unit.Context.ServiceProvider.GetRequiredService<NetOptions>();
        await Console.Out.WriteLineAsync($"Port: {options.Port.ToString()}");

        var netBase = unit.Context.ServiceProvider.GetRequiredService<NetBase>();
        if (CryptoHelper.TryParseFromEnvironmentVariable<NodePrivateKey>("nodekey", out var privateKey))
        {
            netBase.SetNodePrivateKey(privateKey);
        }

        await unit.Run(options, true);

        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.Context.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = false,
        };

        await SimpleParser.ParseAndRunAsync(unit.Context.Commands, args, parserOptions); // Main process

        await unit.Terminate();

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        if (unit.Context.ServiceProvider.GetService<UnitLogger>() is { } unitLogger)
        {
            await unitLogger.FlushAndTerminate();
        }

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
