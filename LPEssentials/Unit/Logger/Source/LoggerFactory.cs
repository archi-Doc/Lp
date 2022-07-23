// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class LoggerFactory<TLogSource> : ILogger<TLogSource>
{
    public LoggerFactory(UnitLogger logger)
    {
        this.logger = logger;
    }

    public ILogger? TryGet(LogLevel logLevel = LogLevel.Information)
        => this.logger.TryGet<TLogSource>(logLevel);

    private readonly UnitLogger logger;
}
