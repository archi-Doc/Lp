// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogOutput
{
    internal delegate void OutputDelegate(Type logSourceType, LogLevel logLevel, string message);

    public void Output(Type logSourceType, LogLevel logLevel, string message);
}
