// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using Arc.Threading;

namespace Arc.Unit;

public class ConsoleLogger : ILogOutput
{
    public ConsoleLogger(ConsoleLoggerOptions options)
    {
        this.worker = new(options.Formatter);
        this.options = options;
    }

    public void Output(LogOutputParameter param)
    {
        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(new(param));
        }
    }

    private ConsoleLoggerWorker worker;
    private ConsoleLoggerOptions options;
}
