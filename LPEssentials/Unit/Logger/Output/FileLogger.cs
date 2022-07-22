// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public class FileLogger : ILogOutput
{
    public FileLogger(FileLoggerOptions options)
    {
        this.worker = new(options.Path, options.Formatter);
        this.options = options;
        this.options.PathFixed = true;
    }

    public void Output(LogOutputParameter param)
    {
        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(new(param));
        }
    }

    public void Flush()
    {
        this.worker.Flush();
    }

    private FileLoggerWorker worker;
    private FileLoggerOptions options;
}
