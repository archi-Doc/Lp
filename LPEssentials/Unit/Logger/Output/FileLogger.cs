// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public class FileLogger<TOption> : BufferedLogOutput
    where TOption : FileLoggerOptions
{
    public FileLogger(UnitLogger unitLogger, TOption options)
        : base(unitLogger)
    {
        this.worker = new(options.Path, options.Formatter);
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

    private FileLoggerWorker worker;
    private TOption options;
}
