// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogger
{
    public void Log(string message);
}

internal class LoggerInstance : ILogger
{
    public LoggerInstance(ILogDestination logDestination, LogLevel logLevel)
    {
        logDestination.Information()
    }

    public void Log(string message)
    {
        throw new NotImplementedException();
    }
}
