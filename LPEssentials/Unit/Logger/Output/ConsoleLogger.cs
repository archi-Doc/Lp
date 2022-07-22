// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using Arc.Threading;

namespace Arc.Unit;

public class ConsoleLogger : BufferedLogOutput
{
    public ConsoleLogger(UnitLogger unitLogger, ConsoleLoggerOptions options)
        : base(unitLogger)
    {
        this.worker = new(options.Formatter);
        this.options = options;
    }

    public override void Output(LogOutputParameter param)
    {
        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(new(param));
        }
    }

    public override Task<int> Flush() => this.worker.Flush();

    private ConsoleLoggerWorker worker;
    private ConsoleLoggerOptions options;
}
