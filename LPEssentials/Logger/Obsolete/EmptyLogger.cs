// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public class EmptyLogger : ISimpleLogger
{
    public EmptyLogger()
    {
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
        this.FatalFlag = true;
    }

    public bool FatalFlag { get; private set; }
}
