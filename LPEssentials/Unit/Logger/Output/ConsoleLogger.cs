// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public class ConsoleLogger : ILogOutput
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

    public void Output(Type logSourceType, LogLevel logLevel, string message)
    {
        Console.WriteLine(message);
    }

    private string format;
}
