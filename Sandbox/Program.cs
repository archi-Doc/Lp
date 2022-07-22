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

internal class TestLogFilter : ILogFilter
{
    public ILogger? Filter(LogFilterParameter param)
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
    public ILogger? Filter(LogFilterParameter param)
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

        Test();
    }

    public static void Test()
    {
        var builder = new LPLogger.Builder()
            .Configure(context =>
            {
                context.AddSingleton<TestLogFilter>();
                context.AddSingleton<AboveInformationFilter>();

                // context.ClearLoggerResolver();
                context.AddLoggerResolver(x =>
                {
                    if (x.LogLevel <= LogLevel.Debug)
                    {
                        x.SetOutput<Arc.Unit.EmptyLogger>();
                        return;
                    }

                    x.SetOutput<FileLogger>();
                    // x.SetOutput<ConsoleLogger>();
                    // x.SetFilter<TestLogFilter>();
                    // x.SetFilter<AboveInformationFilter>();
                });

                // context.Services.Add(ServiceDescriptor.Singleton(typeof(LoggerOption), new Object()));
            });

        var unit = builder.Build();

        /*var options = unit.Context.ServiceProvider.GetRequiredService<ConsoleLoggerOptions>();
        options.MaxQueue = 0;
        options.Formatter.EnableColor = false;
        options.Formatter.TimestampFormat = null;*/

        var logger = unit.Context.ServiceProvider.GetRequiredService<UnitLogger>();
        logger.Get<Program>().Log("Test");
        logger.Get<object>().Log("Test");
        logger.Get<Program>().Log("Test2");
        logger.Get<Program>().Log(1, "test 1");

        logger.Get<DefaultLog>().Log("default");
        logger.Get<DefaultLog>().Log(2, "default2");
        logger.Get<DefaultLog>(LogLevel.Warning).Log(2, "default2");

        var l2 = unit.Context.ServiceProvider.GetRequiredService<ILogger>();
        l2.Log(99, "GetRequiredService");

        var l3 = unit.Context.ServiceProvider.GetRequiredService<ILogger<int>>();
        l3.Log(-1, "GetRequiredService");
        l3 = unit.Context.ServiceProvider.GetRequiredService<ILogger<int>>();
        l3.Log(-1, "GetRequiredService");
        l3.Log(0, "GetRequiredService");
        logger.Get<int>(LogLevel.Warning).Log("aaa");

        for (var i = 0; i < 100; i++)
        {
            logger.Get<Program>(LogLevel.Debug).Log(i, $"test {i}");
        }

        logger.Flush();

        var pass = "pass";
        var encrypted = PasswordEncrypt.Encrypt(new byte[] { 1, 2, }, pass);
        var item = new KeyVaultItem("test.key", PasswordEncrypt.GetPasswordHint(pass), encrypted);
        var array = new KeyVaultItem[] { item, item, };

        var t = Tinyhand.TinyhandSerializer.SerializeToString(array, Tinyhand.TinyhandSerializerOptions.Standard.WithCompose(Tinyhand.TinyhandComposeOption.Standard));

        // ThreadCore.Root.Terminate();
    }
}
