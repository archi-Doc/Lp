// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogger
{
    public void Log(int eventId, string message, Exception? exception = null);

    public Type OutputType { get; }
}

public interface ILogger<TLogSource>
{
    public ILogger? TryGet(LogLevel logLevel = LogLevel.Information);
}
