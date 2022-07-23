// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogger<TLogSource>
{
    public ILogger? TryGet(LogLevel logLevel = LogLevel.Information);
}
