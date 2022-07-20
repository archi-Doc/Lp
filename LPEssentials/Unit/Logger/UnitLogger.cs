// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

public class UnitLogger
{
    private delegate void FilterDelegate(string message);

    public static void Configure(UnitBuilderContext context)
    {
        context.TryAddSingleton<UnitLogger>();

        context.TryAddSingleton<EmptyLogger>();
        context.TryAddSingleton<ConsoleLogger>();
    }

    public UnitLogger(UnitContext context)
    {
        this.serviceProvider = context.ServiceProvider;
        this.loggerResolvers = (LoggerResolverDelegate[])context.LoggerResolvers.Clone();
    }

    public ILogger? TryGet<T>(LogLevel logLevel)
    {
        return this.sourceToLogDelegate.GetOrAdd(new(typeof(T), logLevel), a =>
        {
            LoggerResolverContext context = new(a);
            for (var i = 0; i < this.loggerResolvers.Length; i++)
            {
                this.loggerResolvers[i](context);
                if (context.LogDestinationType != null)
                {
                    if (this.serviceProvider.GetService(context.LogDestinationType) is ILogDestination logDestination)
                    {
                        return new LoggerInstance(logDestination, logLevel);
                    }
                }
            }

            return null;
        });
    }

    public ILogger Get<T>(LogLevel logLevel)
    {
        if (this.TryGet<T>(logLevel) is { } logger)
        {
            return logger;
        }

        throw new LoggerNotFoundException();
    }

    private IServiceProvider serviceProvider;
    private LoggerResolverDelegate[] loggerResolvers;
    private ConcurrentDictionary<LogSourceLevelPair, ILogger?> sourceToLogDelegate = new();
}
