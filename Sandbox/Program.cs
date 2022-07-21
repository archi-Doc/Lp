// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Arc.Crypto;
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

        return param.OriginalLogger;
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
                // context.ClearLoggerResolver();
                context.AddLoggerResolver(x =>
                {
                    // x.SetOutput<ConsoleLogger>();
                    x.SetFilter<TestLogFilter>();
                });

                // context.Services.Add(ServiceDescriptor.Singleton(typeof(LoggerOption), new Object()));
            });

        var unit = builder.Build();

        var logger = unit.Context.ServiceProvider.GetRequiredService<UnitLogger>();
        logger.Get<Program>().Log("Test");
        logger.Get<object>().Log("Test");
        logger.Get<Program>().Log("Test2");

        var pass = "pass";
        var encrypted = PasswordEncrypt.Encrypt(new byte[] { 1, 2, }, pass);
        var item = new KeyVaultItem("test.key", PasswordEncrypt.GetPasswordHint(pass), encrypted);
        var array = new KeyVaultItem[] { item, item, };

        var t = Tinyhand.TinyhandSerializer.SerializeToString(array, Tinyhand.TinyhandSerializerOptions.Standard.WithCompose(Tinyhand.TinyhandComposeOption.Standard));
    }
}
