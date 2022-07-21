// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly struct LogFilterParameter : IEquatable<LogFilterParameter>
{
    public LogFilterParameter(Type logSourceType, LogLevel logLevel, ILogger logger)
    {
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
        this.OriginalLogger = logger;
    }

    public readonly Type LogSourceType;

    public readonly LogLevel LogLevel;

    public readonly ILogger OriginalLogger;

    public bool Equals(LogFilterParameter other)
        => this.LogSourceType == other.LogSourceType &&
        this.LogLevel == other.LogLevel &&
        this.OriginalLogger == other.OriginalLogger;

    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel, this.OriginalLogger);
}
