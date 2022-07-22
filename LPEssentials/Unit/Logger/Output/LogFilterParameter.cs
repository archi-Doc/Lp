// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly struct LogOutputParameter : IEquatable<LogOutputParameter>
{
    public LogOutputParameter(Type logSourceType, LogLevel logLevel, int eventId, string message, Exception? exception)
    {
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
        this.EventId = eventId;
        this.Message = message;
        this.Exception = exception;
    }

    public readonly Type LogSourceType;

    public readonly LogLevel LogLevel;

    public readonly int EventId;

    public readonly string Message;

    public readonly Exception? Exception;

    public bool Equals(LogOutputParameter other)
        => this.LogSourceType == other.LogSourceType &&
        this.LogLevel == other.LogLevel &&
        this.EventId == other.EventId &&
        this.Message == other.Message &&
        this.Exception == other.Exception;

    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel, this.EventId, this.Message, this.Exception);
}
