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
        AppCloseHandler.Register(() =>
        {// Closing the console window or terminating the process.
            root?.RequestTermination(); // Send a termination signal to the root.
            root?.WaitForTerminationAsync(TimeSpan.FromSeconds(2)).Wait();
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

                context.AddLogOutputResolver(context =>
                {// Logger
                    if (context.LogLevel == LogLevel.Debug)
                    {
                        context.SetOutput<FileLogOutput<FileLogOutputOptions>>();
                        return;
                    }

                    context.SetOutput<ConsoleAndFileLogOutput>();
                });
            })
             .ConfigureNetsphere(context =>
             {// Register the services provided by the server.
                 context.AddNetService<ITestService, TestServiceImpl>();
             })
             .PostConfigure(context =>
             {
                 {// FileLogOutputOptions
                     var logfile = "Logs/Debug.txt";
                     var options = context.GetOrCreateOptions<FileLogOutputOptions>();
                     options = options with
                     {
                         FilePath = Path.Combine(context.DataDirectory, logfile),
                         MaxLogCapacityInMegabytes = 1,
                         FormatterOptions = options.FormatterOptions with { TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff K", },
                         ClearLogsAtStartup = true,
                         MaxQueueLength = 100_000,
                     };

                     context.SetOptions(options);
                 }

                 {// NetOptions
                     var options = context.GetOrCreateOptions<NetOptions>();
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
            RequireCommandName = false,
            RejectUnknownOptionNames = false,
        };

        await SimpleParser.ParseAndExecute(unit.Context.CommandTypes, SimpleParserHelper.GetCommandLineArguments(), parserOptions); // Main process

        await unit.Terminate();

        root.RequestTermination();
        if (unit.Context.ServiceProvider.GetService<LogUnit>() is { } unitLogger)
        {
            await unitLogger.FlushAndTerminateAsync();
        }

        await root.WaitForTerminationAsync(); // Wait for the termination infinitely.
    }
}
