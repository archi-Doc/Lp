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

    public void Debug(string message)
    {
        this.Output(message);
    }

    public void Information(string message)
    {
        this.Output(message);
    }

    public void Warning(string message)
    {
        this.Output(message);
    }

    public void Error(string message)
    {
        this.Output(message);
    }

    public void Fatal(string message)
    {
        this.Output(message);
    }

    private void Output(string message) => Console.WriteLine(message);

    private string format;
}
