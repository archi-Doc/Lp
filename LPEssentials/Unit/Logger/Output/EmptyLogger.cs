// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public class EmptyLogger : ILogOutput
{
    public EmptyLogger()
    {
    }

    public void Output(Type logSourceType, LogLevel logLevel, string message)
    {
    }
}
