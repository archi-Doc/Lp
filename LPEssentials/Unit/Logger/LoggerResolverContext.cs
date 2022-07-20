// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public sealed class LoggerResolverContext
{
    public LoggerResolverContext(LogSourceLevelPair pair)
    {
        this.LogSourceType = pair.LogSourceType;
        this.LogLevel = pair.LogLevel;

    }

    public LoggerResolverContext(Type logSourceType, LogLevel logLevel)
    {
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
    }

    public void SetLogDestination<TLogDestination>()
        where TLogDestination : ILogDestination
    {
        this.LogDestinationType = typeof(TLogDestination);
    }

    public void SetLogDestinationType(Type logDestinationType)
    {
        if (logDestinationType.GetInterfaces().Contains(typeof(ILogDestination)))
        {
            throw new InvalidOperationException();
        }

        this.LogDestinationType = logDestinationType;
    }

    public void SetLogFilter<TLogFilter>()
        where TLogFilter : ILogFilter
    {
        this.LogFilterType = typeof(TLogFilter);
    }

    public void SetLogFilterType(Type logFilterType)
    {
        if (logFilterType.GetInterfaces().Contains(typeof(ILogFilter)))
        {
            throw new InvalidOperationException();
        }

        this.LogFilterType = logFilterType;
    }

    public Type LogSourceType { get; }

    public LogLevel LogLevel { get; }

    public Type? LogDestinationType { get; private set; }

    public Type? LogFilterType { get; private set; }
}
