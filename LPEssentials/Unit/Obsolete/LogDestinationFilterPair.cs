// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit.Obsolete;

/*public class LogResolverResult<TLogDestination>
    where TLogDestination : ILogDestination
{
    public LogResolverResult(LogLevel logLevel)
    {
        this.LogDestinationType = typeof(TLogDestination);
        this.LogLevel = logLevel;
    }

    public readonly Type? LogDestinationType;

    public readonly LogLevel LogLevel;
}*/

/*public readonly struct LogResolverResult : IEquatable<LogResolverResult>
{
    public LogResolverResult<TLogDestination>(LogLevel logLevel)
        where TLogDestination : ILogDestination
    {
        this.LogDestinationType = typeof(TLogDestination);
        this.LogLevel = logLevel;
    }

    public readonly Type? LogDestinationType;

    public readonly LogLevel LogLevel;

    public bool Equals(LogResolverResult other)
        => this.LogDestinationType == other.LogDestinationType &&
            this.LogLevel == other.LogLevel;

    public override int GetHashCode()
        => HashCode.Combine(this.LogDestinationType, this.LogLevel);
}*/
/*
public readonly partial struct LogDestinationFilterPair : IEquatable<LogDestinationFilterPair>
{
    public LogDestinationFilterPair(Type? logDestinationType, LogLevel logLevel)
    {
        this.LogDestinationType = logDestinationType;
        this.LogLevel = logLevel;
    }

    public LogDestinationFilterPair<T>(int x)
    {

    }

public readonly Type? LogDestinationType;

public readonly LogLevel LogLevel;

public bool Equals(LogDestinationFilterPair other)
    => this.LogDestinationType == other.LogDestinationType &&
        this.LogLevel == other.LogLevel;

public override int GetHashCode()
    => HashCode.Combine(this.LogDestinationType, this.LogLevel);
}
*/
