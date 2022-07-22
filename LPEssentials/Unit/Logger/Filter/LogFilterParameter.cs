﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly struct LogFilterParameter : IEquatable<LogFilterParameter>
{
    public LogFilterParameter(ILogContext context, Type logSourceType, LogLevel logLevel, int eventId, ILogger originalLogger)
    {
        this.Context = context;
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
        this.EventId = eventId;
        this.OriginalLogger = originalLogger;
    }

    public readonly ILogContext Context;

    public readonly Type LogSourceType;

    public readonly LogLevel LogLevel;

    public readonly int EventId;

    public readonly ILogger OriginalLogger;

    public bool Equals(LogFilterParameter other)
        => this.LogSourceType == other.LogSourceType &&
        this.LogLevel == other.LogLevel &&
        this.EventId == other.EventId &&
        this.OriginalLogger == other.OriginalLogger;

    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel, this.EventId, this.OriginalLogger);
}
