// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILoggerSource
{
    public ILogger? TryGet(LogLevel logLevel = LogLevel.Information);
}

public interface ILoggerSource<TLogSource> : ILoggerSource
{
}
