﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using System;
global using System.Threading;
global using System.Threading.Tasks;
global using Arc.Threading;
global using Netsphere;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Logging;
using LP.NetServices;
using SimpleCommandLine;

namespace NetsphereTest;

public class Program
{
    // basic -node alternative -ns{-alternative true -logger true}
    // stress -ns{-alternative true -logger true}

    public static async Task Main(string[] args)
    {
        // 1st: DI Container
        /*var commandTypes = new List<Type>();
        commandTypes.Add(typeof(BasicTestSubcommand));
        commandTypes.Add(typeof(NetbenchSubcommand));

        NetControl.Register(Container, commandTypes);
        foreach (var x in commandTypes)
        {
            Container.Register(x, Reuse.Singleton);
        }

        // Services
        Container.Register<ExternalServiceImpl>(Reuse.Singleton);

        Container.Register<TestFilterB>(Reuse.Singleton);

        Container.ValidateAndThrow();*/

        // 2nd: ServiceCollection
        /*var services = new ServiceCollection();
        NetControl.Register(services, commandTypes);
        foreach (var x in commandTypes)
        {
            services.AddSingleton(x);
        }

        services.AddSingleton<ExternalServiceImpl>();

        services.AddSingleton<TestFilterB>();

        var serviceProvider = services.BuildServiceProvider();
        NetControl.SetServiceProvider(serviceProvider);*/

        // NetControl.QuickStart(true, () => new TestServerContext(), () => new TestCallContext(), "test", options, true);

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

        SimpleParserHelper.AddEnvironmentVariable(ref args, "lpargs");
        if (args.Length == 0)
        {
            Console.Write("Arguments: ");
            var arguments = Console.ReadLine();
            if (arguments != null)
            {
                args = new string[1] { arguments, };
            }
        }

        // 3rd: Builder pattern
        var builder = new NetControl.Builder()
            .Preload(context =>
            {
                var original = context.GetOrCreateOptions<NetsphereOptions>();
                original.EnableAlternative = true;
                original.Port = 49152;

                NetsphereOptions? options = default;
                if (context.Arguments.TryGetOption("ns", out var nsArg))
                {
                    SimpleParser.TryParseOptions(nsArg.UnwrapBracket(), out options, original);
                }

                options ??= original;
                context.SetOptions(options);
            })
            .Configure(context =>
            {
                // Command
                context.AddCommand(typeof(DeliveryTestSubcommand));
                context.AddCommand(typeof(BasicTestSubcommand));
                context.AddCommand(typeof(NetbenchSubcommand));
                context.AddCommand(typeof(TaskScalingSubcommand));
                context.AddCommand(typeof(StressSubcommand));
                context.AddCommand(typeof(RemoteBenchSubcommand));
                context.AddCommand(typeof(UdpRecvSubcommand));
                // context.AddCommand(typeof(UdpSendSubcommand));

                // NetService
                context.AddSingleton<BenchmarkServiceImpl>();
                context.AddSingleton<ExternalServiceImpl>();

                // ServiceFilter
                context.AddSingleton<LP.NetServices.TestFilterB>();

                // Other
                context.AddSingleton<RemoteBenchBroker>();

                // Resolver
                context.ClearLoggerResolver();
                context.AddLoggerResolver(context =>
                {
                    if (context.LogLevel == LogLevel.Debug)
                    {
                        context.SetOutput<FileLogger<FileLoggerOptions>>();
                        return;
                    }

                    /*if (context.LogSourceType == typeof(ClientTerminal))
                    {// ClientTerminal
                        context.SetOutput<StreamLogger<ClientTerminalLoggerOptions>>();
                        return;
                    }
                    else if (context.LogSourceType == typeof(ServerTerminal))
                    {// ServerTerminal
                        context.SetOutput<StreamLogger<ServerTerminalLoggerOptions>>();
                        return;
                    }
                    else if (context.LogSourceType == typeof(Terminal))
                    {// Terminal
                        context.SetOutput<StreamLogger<TerminalLoggerOptions>>();
                        return;
                    }
                    else*/
                    {
                        context.SetOutput<ConsoleLogger>();
                    }
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
            });

        Console.WriteLine(string.Join(' ', args));

        var unit = builder.Build(args);
        if (args[0] == "udpsend" || args[0] == "udprecv")
        {
            goto RunAsync;
        }

        var options = unit.Context.ServiceProvider.GetRequiredService<NetsphereOptions>();
        options.EnableServer = true;
        await Console.Out.WriteLineAsync(options.ToString());
        await unit.Run(options, true);

        var netControl = unit.Context.ServiceProvider.GetRequiredService<NetControl>();
        netControl.Services.Register<IBenchmarkService>();

RunAsync:
        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.Context.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = false,
        };

        await SimpleParser.ParseAndRunAsync(unit.Context.Commands, args, parserOptions); // Main process

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
