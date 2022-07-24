// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

public class FileLogger<TOption> : BufferedLogOutput
    where TOption : FileLoggerOptions
{
    public FileLogger(UnitCore core, UnitLogger unitLogger, TOption options)
        : base(unitLogger)
    {
        if (string.IsNullOrEmpty(Path.GetDirectoryName(options.Path)))
        {// tempcode
            options.Path = Path.Combine(Directory.GetCurrentDirectory(), options.Path);
        }

        this.worker = new(core, unitLogger, options.Path, options.MaxLogCapacity, options.Formatter);
        this.options = options;
    }

    public override void Output(LogOutputParameter param)
    {
        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(new(param));
        }
    }

    public override Task<int> Flush(bool terminate) => this.worker.Flush(terminate);

    private FileLoggerWorker worker;
    private TOption options;
}
