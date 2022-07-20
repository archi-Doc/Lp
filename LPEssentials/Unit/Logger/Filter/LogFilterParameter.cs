// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly struct LogFilterParameter : IEquatable<LogFilterParameter>
{
    public LogFilterParameter(Type logSourceType, LogLevel logLevel, ILogger logger)
    {
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
        this.Logger = logger;
    }

    public readonly Type LogSourceType;

    public readonly LogLevel LogLevel;

    public readonly ILogger Logger;

    public bool Equals(LogFilterParameter other)
        => this.LogSourceType == other.LogSourceType &&
        this.LogLevel == other.LogLevel &&
        this.Logger == other.Logger;

    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel, this.Logger);
}
