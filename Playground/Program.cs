// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using Arc.Threading;
global using CrystalData;
global using Tinyhand;
using Arc;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using Netsphere.Crypto;
using Netsphere.Relay;
using SimpleCommandLine;

namespace Playground;

public class Program
{
    private static ExecutionRoot? root;

    public static async Task Main()
    {
        AppCloseHandler.Set(() =>
        {// Closing the console window or terminating the process.
            root?.RequestTermination(); // Send a termination signal to the root.
            root?.WaitForTermination(TimeSpan.FromSeconds(2)).Wait();
        });

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed.
            e.Cancel = true;
            root?.RequestTermination(); // Send a termination signal to the root.
        };

        var builder = new NetUnit.Builder()
            .Configure(context =>
            {
                context.AddSingleton<IRelayControl, CertificateRelayControl>();

                // Command
                context.AddCommand(typeof(RelayCommand));
                context.AddCommand(typeof(BasicCommand));

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
             .ConfigureNetsphere(context =>
             {// Register the services provided by the server.
                 context.AddNetService<ITestService, TestServiceImpl>();
             })
             .PostConfigure(context =>
             {
                 {// FileLoggerOptions
                     var logfile = "Logs/Debug.txt";
                     var options = context.GetOptions<FileLoggerOptions>();
                     options = options with
                     {
                         Path = Path.Combine(context.DataDirectory, logfile),
                         MaxLogCapacity = 1,
                         FormatterOptions = options.FormatterOptions with { TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff K", },
                         ClearLogsAtStartup = true,
                         MaxQueue = 100_000,
                     };

                     context.SetOptions(options);
                 }

                 {// NetOptions
                     var options = context.GetOptions<NetOptions>();
                     options = options with
                     {
                         NodeName = "test",
                         EnablePing = true,
                         EnableServer = true,
                         EnableAlternative = true,
                     };

                     context.SetOptions(options);
                 }
             });

        // Netsphere
        var unit = builder.Build();
        root = unit.Context.ExecutionRoot;
        var options = unit.Context.ServiceProvider.GetRequiredService<NetOptions>();
        await Console.Out.WriteLineAsync($"Port: {options.Port.ToString()}");

        var netBase = unit.Context.ServiceProvider.GetRequiredService<NetBase>();
        if (BaseHelper.TryParseFromEnvironmentVariable<SeedKey>("nodesecretkey", out var seedKey))
        {
            netBase.SetNodeSeedKey(seedKey);
        }

        await unit.Run(options, true);

        var parserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = unit.Context.ServiceProvider,
            RequireStrictCommandName = false,
            RequireStrictOptionName = false,
        };

        await SimpleParser.ParseAndExecute(unit.Context.Commands, SimpleParserHelper.GetCommandLineArguments(), parserOptions); // Main process

        await unit.Terminate();

        root.RequestTermination();
        if (unit.Context.ServiceProvider.GetService<LogUnit>() is { } unitLogger)
        {
            await unitLogger.FlushAndTerminate();
        }

        await root.WaitForTermination(); // Wait for the termination infinitely.
    }
}
