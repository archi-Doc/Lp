// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly partial struct LogSourceLevelPair : IEquatable<LogSourceLevelPair>
{
    public LogSourceLevelPair(Type logSourceType, LogLevel logLevel)
    {
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
    }

    public readonly Type LogSourceType;

    public readonly LogLevel LogLevel;

    public bool Equals(LogSourceLevelPair other)
        => this.LogSourceType == other.LogSourceType &&
            this.LogLevel == other.LogLevel;

    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel);
}
