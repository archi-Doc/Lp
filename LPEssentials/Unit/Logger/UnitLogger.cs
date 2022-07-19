// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

public class UnitLogger
{
    public UnitLogger(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public ILogger? TryGet<T>(LogLevel logLevel)
    {
        return this.dictionary.GetOrAdd(new(typeof(T), logLevel), a =>
        {
            for (var i = 0; i < this.loggerResolvers.Length; i++)
            {
                if (this.loggerResolvers[i](a) is { } logDestinationType)
                {
                    if (this.serviceProvider.GetService(logDestinationType) is ILogDestination logDestination)
                    {
                        return logger;
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
    private Func<LogSourceLevelPair, Type?>[] loggerResolvers;
    private ConcurrentDictionary<LogSourceLevelPair, ILogger?> dictionary = new();
}
