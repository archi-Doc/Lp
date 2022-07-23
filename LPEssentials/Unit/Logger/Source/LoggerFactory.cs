// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class LoggerSourceFactory<TLogSource> : ILoggerSource<TLogSource>
{
    public LoggerSourceFactory(UnitLogger logger)
    {
        this.logger = logger;
    }

    public ILogger? TryGet(LogLevel logLevel = LogLevel.Information)
        => this.logger.TryGet<TLogSource>(logLevel);

    private readonly UnitLogger logger;
}
