// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using System;
global using System.Threading;
global using System.Threading.Tasks;
global using Arc.Threading;
global using Netsphere;
using Arc.Unit;
using LP.Data;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Logging;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace NetsphereTest;

public class Program
{
    // public static Container Container { get; } = new();

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

        Console.Write("Arguments: ");
        var arguments = Console.ReadLine();
        if (arguments != null)
        {
            args = new string[1] { arguments, };
        }

        // 3rd: Builder pattern
        var builder = new NetControl.Builder()
            .Configure(context =>
            {
                // Command
                context.AddCommand(typeof(BasicTestSubcommand));
                context.AddCommand(typeof(NetbenchSubcommand));
                context.AddCommand(typeof(TaskScalingSubcommand));
                context.AddCommand(typeof(StressSubcommand));

                // NetService
                context.AddSingleton<ExternalServiceImpl>();

                // ServiceFilter
                context.AddSingleton<LP.NetServices.TestFilterB>();

                // Resolver
                context.ClearLoggerResolver();
                context.AddLoggerResolver(context =>
                {
                    if (context.LogLevel == LogLevel.Debug)
                    {// Debug -> no output
                        context.SetOutput<EmptyLogger>();
                        return;
                    }

                    if (context.LogSourceType == typeof(ClientTerminal))
                    {// ClientTerminal
                        context.SetOutput<StreamLogger<ClientTerminalLoggerOptions>>();
                        return;
                    }
                    else if (context.LogSourceType == typeof(ServerTerminal))
                    {// ServerTerminal
                        context.SetOutput<StreamLogger<ServerTerminalLoggerOptions>>();
                        return;
                    }
                });
            })
            .SetupOptions<ClientTerminalLoggerOptions>((context, options) =>
            {// ClientTerminalLoggerOptions
                var logfile = "Logs/Client/.txt";
                options.Path = Path.Combine(context.RootDirectory, logfile);
                options.MaxLogCapacity = 1;
                options.MaxStreamCapacity = 1_000;
            })
            .SetupOptions<ServerTerminalLoggerOptions>((context, options) =>
            {// ServerTerminalLoggerOptions
                var logfile = "Logs/Server/.txt";
                options.Path = Path.Combine(context.RootDirectory, logfile);
                options.MaxLogCapacity = 1;
                options.MaxStreamCapacity = 1_000;
            });

        var options = new LP.Data.NetsphereOptions();
        options.EnableAlternative = true;
        options.EnableLogger = false;

        var unit = builder.Build();
        var param = new NetControl.Unit.Param(true, () => new TestServerContext(), () => new TestCallContext(), "test", options, true);
        await unit.RunStandalone(param);

        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.Context.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = true,
        };

        // await SimpleParser.ParseAndRunAsync(commandTypes, "netbench -node alternative", parserOptions); // Main process
        // SimpleParserHelper.AddEnvironmentVariable(ref args, "lpargs");
        await SimpleParser.ParseAndRunAsync(unit.Context.Commands, args, parserOptions); // Main process

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
