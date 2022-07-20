// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogger
{
    internal delegate void LogDelegate(string message);

    public void Log(string message);
}

internal class LoggerInstance : ILogger
{
    public LoggerInstance(ILogDestination logDestination, ILogFilter? logFilter, LogLevel logLevel)
    {
        var type = logDestination.GetType();
        var method = type.GetMethod(logLevel.ToString());
        if (method == null)
        {
            throw new ArgumentException();
        }

        this.logDelegate = (ILogger.LogDelegate)Delegate.CreateDelegate(type, logDestination, method);

        if (logFilter != null)
        {
            type = logFilter.GetType();
            method = type.GetMethod(nameof(ILogFilter.Filter));
            if (method == null)
            {
                throw new ArgumentException();
            }

            this.filterDelegate = (ILogFilter.FilterDelegate)Delegate.CreateDelegate(type, logFilter, method);
        }
    }

    public void Log(string message)
    {
        if (this.filterDelegate != null)
        {
            var dest = this.filterDelegate(logSource, logLevel, logDestination);
        }
        this.logDelegate(message);
    }

    private ILogger.LogDelegate logDelegate;
    private ILogFilter.FilterDelegate? filterDelegate;

}
