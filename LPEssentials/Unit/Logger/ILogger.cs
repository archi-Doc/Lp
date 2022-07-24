// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogger
{
    public ILog? TryGet(LogLevel logLevel = LogLevel.Information);
}

public interface ILogger<TLogSource> : ILogger
{
}
