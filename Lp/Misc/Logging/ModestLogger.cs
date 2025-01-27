// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Logging;

/// <summary>
/// Logger with repetition suppression and suppression time function.
/// </summary>
public class ModestLogger
{
    private readonly Dictionary<ulong, DateTime> identifierToDateTime = new();
    private ILogger logger;
    private ulong lastIdentifier;
    private DateTime suppressionTime;

    public ModestLogger(ILogger logger)
    {
        this.logger = logger;
    }

    public void SetLogger(ILogger logger)
    {
        this.logger = logger;
    }

    public void SetSuppressionTime(TimeSpan duration)
    {
        if (duration == default)
        {
            this.suppressionTime = default;
        }
        else
        {
            this.suppressionTime = DateTime.UtcNow + duration;
        }
    }

    /// <summary>
    /// Logs a message if the provided identifier is not the same as the last logged identifier.
    /// </summary>
    /// <param name="identifier">The identifier to check.</param>
    /// <param name="logLevel">The log level for the message.</param>
    /// <returns>An <see cref="ILogWriter"/> if the identifier is different; otherwise, <c>null</c>.</returns>
    public ILogWriter? NonConsecutive(ulong identifier, LogLevel logLevel = LogLevel.Information)
    {
        if (DateTime.UtcNow < this.suppressionTime)
        {// Suppression time
            return null;
        }
        else if (this.lastIdentifier == identifier)
        {// Same identifier
            return null;
        }
        else
        {
            this.lastIdentifier = identifier;
            return this.logger.TryGet(logLevel);
        }
    }

    public ILogWriter? Interval(TimeSpan interval, ulong identifier, LogLevel logLevel = LogLevel.Information)
    {
        var utcNow = DateTime.UtcNow;
        if (utcNow < this.suppressionTime)
        {// Suppression time
            return null;
        }
        else
        {
            if (this.identifierToDateTime.TryGetValue(identifier, out var lastDateTime))
            {
                if (utcNow < lastDateTime + interval)
                {
                    return null;
                }
            }

            this.identifierToDateTime[identifier] = utcNow;

            this.lastIdentifier = identifier;
            return this.logger.TryGet(logLevel);
        }
    }
}
