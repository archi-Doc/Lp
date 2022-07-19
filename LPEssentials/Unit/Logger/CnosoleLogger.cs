// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public class ConsoleLogger : ILogDestination
{
    public const string DefaultFormat = "";

    public ConsoleLogger()
    {
        this.format = DefaultFormat;
    }

    public ConsoleLogger(string format)
    {
        this.format = format;
    }

    public void Debug(string message)
    {
    }

    public void Information(string message)
    {
    }

    public void Warning(string message)
    {
    }

    public void Error(string message)
    {
    }

    public void Fatal(string message)
    {
    }

    private string format;
}
