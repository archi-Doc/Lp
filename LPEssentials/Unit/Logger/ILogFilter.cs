// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogFilter
{
    internal delegate ILogDestination? FilterDelegate(ILogSource logSource, LogLevel logLevel, ILogDestination logDestination);

    ILogDestination? Filter(ILogSource logSource, LogLevel logLevel, ILogDestination logDestination);
}
