// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;

namespace Arc.Unit;

public class UnitLogger
{
    public static void Configure(UnitBuilderContext context)
    {
        // Main
        context.TryAddSingleton<UnitLogger>();
        context.Services.Add(ServiceDescriptor.Transient<ILogger>(x => x.GetService<UnitLogger>()?.Get<DefaultLog>() ?? throw new LoggerNotFoundException(typeof(DefaultLog), LogLevel.Information)));
        context.Services.Add(ServiceDescriptor.Transient(typeof(ILogger<>), typeof(LoggerFactory<>)));

        // Empty
        context.TryAddSingleton<EmptyLogger>();

        // Console
        context.TryAddSingleton<ConsoleLogger>();
        context.TryAddSingleton<ConsoleLoggerOptions>();

        // Console
        context.TryAddSingleton<FileLogger>();
        context.TryAddSingleton<FileLoggerOptions>();

        // Default resolver
        context.AddLoggerResolver(x =>
        {
            x.SetOutput<ConsoleLogger>();
        });
    }

    private class LogContext : ILogContext
    {
        public LogContext(UnitLogger unitLogger)
        {
            this.unitLogger = unitLogger;
        }

        public ILogger? TryGet<TLogOutput>()
        {
            return this.unitLogger.sourceLevelToLogger.GetOrAdd(new(typeof(TLogOutput), LogLevel.Information), x =>
            {
                if (this.unitLogger.serviceProvider.GetService(x.LogSourceType) is ILogOutput logOutput)
                {
                    return new LoggerInstance(this, null!, x.LogLevel, logOutput, null);
                }

                return null;
            });
        }

        private UnitLogger unitLogger;
    }

    public UnitLogger(UnitContext context)
    {
        this.context = new(this);
        this.serviceProvider = context.ServiceProvider;
        this.loggerResolvers = (LoggerResolverDelegate[])context.LoggerResolvers.Clone();
    }

    public ILogger? TryGet<TLogSource>(LogLevel logLevel = LogLevel.Information)
    // where TLogSource : ILogSource
    {
        return this.sourceLevelToLogger.GetOrAdd(new(typeof(TLogSource), logLevel), x =>
        {
            LoggerResolverContext context = new(x);
            for (var i = 0; i < this.loggerResolvers.Length; i++)
            {
                this.loggerResolvers[i](context);
            }

            if (context.LogOutputType != null)
            {
                if (this.serviceProvider.GetService(context.LogOutputType) is ILogOutput logOutput)
                {
                    var logFilter = context.LogFilterType == null ? null : (ILogFilter)this.serviceProvider.GetRequiredService(context.LogFilterType);
                    return new LoggerInstance(this.context, x.LogSourceType, x.LogLevel, logOutput, logFilter);
                }
            }

            return null;
        });
    }

    public ILogger Get<TLogSource>(LogLevel logLevel = LogLevel.Information)
    // where TLogSource : ILogSource
    {
        if (this.TryGet<TLogSource>(logLevel) is { } logger)
        {
            return logger;
        }

        throw new LoggerNotFoundException(typeof(TLogSource), logLevel);
    }

    public void Flush()
    {
        var options = this.serviceProvider.GetService<FileLoggerOptions>();
        if (options?.PathFixed == true)
        {
            var logger = this.serviceProvider.GetService<FileLogger>();
            logger?.Flush();
        }
    }

    private LogContext context;
    private IServiceProvider serviceProvider;
    private LoggerResolverDelegate[] loggerResolvers;
    private ConcurrentDictionary<LogSourceLevelPair, ILogger?> sourceLevelToLogger = new();
}
