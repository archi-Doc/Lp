// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class LoggerFactory<TLogSource> : ILogger<TLogSource>
{
    public LoggerFactory(UnitLogger unitLogger)
    {
        this.logger = unitLogger.Get<TLogSource>();
    }

    public Type OutputType
        => this.logger.OutputType;

    public void Log(int eventId, string message, Exception? exception)
        => this.logger.Log(eventId, message, exception);

    private readonly ILogger logger;
}
