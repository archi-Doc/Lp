// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Arc.Crypto;
using Arc.Threading;
using Arc.Unit;
using LP;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;

namespace Sandbox;

internal class FileLoggerOptions2 : FileLoggerOptions
{
}

internal class ConsoleAndFileLogger : ILogOutput
{
    public ConsoleAndFileLogger(ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger, FileLogger<FileLoggerOptions2> fileLogger2)
    {
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
        this.fileLogger2 = fileLogger2;
    }

    public void Output(LogOutputParameter param)
    {
        this.consoleLogger.Output(param);
        this.fileLogger.Output(param);
        this.fileLogger2.Output(param);
    }

    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
    private FileLogger<FileLoggerOptions2> fileLogger2;
}

internal class TestLogFilter : ILogFilter
{
    public ILog? Filter(LogFilterParameter param)
    {
        if (param.LogSourceType == typeof(object))
        {
            return null;
        }
        else if (param.LogSourceType == typeof(int))
        {
            if (param.EventId != 0)
            {
                return null;
            }

            return param.Context.TryGet<Arc.Unit.EmptyLogger>();
        }

        return param.OriginalLogger;
    }
}

internal class AboveInformationFilter : ILogFilter
{
    public ILog? Filter(LogFilterParameter param)
    {
        if (param.LogLevel >= LogLevel.Information)
        {
            return param.OriginalLogger;
        }

        return null;
    }
}

internal class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Sandbox");

        await Test(args);
    }

    public static async Task Test(string[] args)
    {
        var builder = new UnitBuilder() // new LP.Logging.LPLogger.Builder()
            .Preload(context =>
            {
                var b = context.Arguments.ContainsOption("help");
                b = context.Arguments.TryGetOption("help", out var value);
            })
            .Configure(context =>
            {
                // Loggers
                context.AddSingleton<ConsoleAndFileLogger>();

                // Options
                context.AddSingleton<FileLoggerOptions2>();

                // Filters
                context.AddSingleton<TestLogFilter>();
                context.AddSingleton<AboveInformationFilter>();

                // context.ClearLoggerResolver();
                context.AddLoggerResolver(x =>
                {
                    if (x.LogLevel <= LogLevel.Debug)
                    {
                        x.SetOutput<Arc.Unit.FileLogger<FileLoggerOptions>>();
                        return;
                    }

                    x.SetOutput<ConsoleAndFileLogger>();
                    // x.SetOutput<ConsoleLogger>();
                    // x.SetFilter<TestLogFilter>();
                    // x.SetFilter<AboveInformationFilter>();
                });

                // context.Services.Add(ServiceDescriptor.Singleton(typeof(LoggerOption), new Object()));
            })
            .SetupOptions<FileLoggerOptions>((context, options) =>
            {
                options.Path = Path.Combine(context.RootDirectory, "Logs/Logs.txt");
            });
            /*.ConfigureOptions<FileLoggerOptions2>(options =>
            {
                options.Path = "Log2.txt";
            });*/

        var unit = builder.Build("helP me -heLP 234 -roOt \"test\"");

        /*var options = unit.Context.ServiceProvider.GetRequiredService<ConsoleLoggerOptions>();
        options.MaxQueue = 0;
        options.Formatter.EnableColor = false;
        options.Formatter.TimestampLocal = false;*/

        var options2 = unit.Context.ServiceProvider.GetRequiredService<FileLoggerOptions2>();
        options2.Path = "LogLog.txt";

        var logger = unit.Context.ServiceProvider.GetRequiredService<UnitLogger>();
        logger.Get<Program>(LogLevel.Warning).Log("Test");
        logger.Get<object>(LogLevel.Error).Log("Test");
        logger.Get<Program>(LogLevel.Fatal).Log("Test2");
        logger.Get<Program>().Log(1, "test 1");

        Thread.Sleep(10);
        logger.Get<DefaultLog>().Log("default");
        Thread.Sleep(20);
        logger.Get<DefaultLog>().Log(2, "default2");
        Thread.Sleep(30);
        logger.Get<DefaultLog>(LogLevel.Warning).Log(2, "default2");

        var l2 = unit.Context.ServiceProvider.GetRequiredService<ILog>();
        l2.Log(99, "GetRequiredService");

        var l3 = unit.Context.ServiceProvider.GetRequiredService<ILogger<int>>();
        l3.TryGet()?.Log(-1, "GetRequiredService");
        l3 = unit.Context.ServiceProvider.GetRequiredService<ILogger<int>>();
        l3.TryGet()?.Log(-1, "GetRequiredService");
        l3.TryGet()?.Log(0, "GetRequiredService");
        logger.Get<int>(LogLevel.Warning).Log("aaa");

        unit.Context.ServiceProvider.GetService<FileLogger<FileLoggerOptions>>();
        unit.Context.ServiceProvider.GetService<FileLogger<FileLoggerOptions>>();

        for (var i = 0; i < 100; i++)
        {
            logger.Get<Program>(LogLevel.Debug).Log(i, $"test {i}");
        }

        await logger.Flush();

        var pass = "pass";
        var encrypted = PasswordEncrypt.Encrypt(new byte[] { 1, 2, }, pass);
        var item = new KeyVaultItem("test.key", PasswordEncrypt.GetPasswordHint(pass), encrypted);
        var array = new KeyVaultItem[] { item, item, };

        var t = Tinyhand.TinyhandSerializer.SerializeToString(array);

        // ThreadCore.Root.Terminate();
    }
}
