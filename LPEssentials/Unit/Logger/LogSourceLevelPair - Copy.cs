// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly partial struct LogDestinationLevelPair : IEquatable<LogDestinationLevelPair>
{
    public LogDestinationLevelPair(Type logDestinationType, LogLevel logLevel)
    {
        this.LogDestinationType = logDestinationType;
        this.LogLevel = logLevel;
    }

    public readonly Type LogDestinationType;

    public readonly LogLevel LogLevel;

    public bool Equals(LogDestinationLevelPair other)
        => this.LogDestinationType == other.LogDestinationType &&
            this.LogLevel == other.LogLevel;

    public override int GetHashCode()
        => HashCode.Combine(this.LogDestinationType, this.LogLevel);
}
