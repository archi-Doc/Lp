// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using Arc.Threading;

namespace Arc.Unit;

public class ConsoleLogger : BufferedLogOutput
{
    public ConsoleLogger(UnitCore core, UnitLogger unitLogger, ConsoleLoggerOptions options)
        : base(unitLogger)
    {
        this.Formatter = new(options.Formatter);
        if (options.EnableBuffering)
        {
            this.worker = new(core, this);
        }

        this.options = options;
    }

    public override void Output(LogOutputParameter param)
    {
        if (this.worker == null)
        {
            Console.WriteLine(this.Formatter.Format(param));
            return;
        }

        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(new(param));
        }
    }

    public override Task<int> Flush(bool terminate) => this.worker?.Flush(terminate) ?? Task.FromResult(0);

    internal SimpleLogFormatter Formatter { get; init; }

    private ConsoleLoggerWorker? worker;
    private ConsoleLoggerOptions options;
}
