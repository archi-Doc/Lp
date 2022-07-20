// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public class LoggerNotFoundException : Exception
{
    public LoggerNotFoundException(Type sourceType, LogLevel logLevel)
        : base($"Logger is not found for LogSource: {sourceType.ToString()} LogLevel: {logLevel.ToString()}")
    {
    }
}
