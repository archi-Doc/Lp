﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

public class ClientTerminalLoggerOptions : StreamLoggerOptions
{
}

public class ServerTerminalLoggerOptions : StreamLoggerOptions
{
}

public class StreamLoggerOptions : FileLoggerOptions
{
    /// <summary>
    /// Gets or sets the upper limit of log stream.
    /// </summary>
    public int MaxStreamCapacity { get; set; } = 10;
}

internal class StreamLogger<TOption> : BufferedLogOutput
    where TOption : StreamLoggerOptions
{
    public StreamLogger(UnitCore core, UnitLogger unitLogger, TOption options)
        : base(unitLogger)
    {
        if (string.IsNullOrEmpty(Path.GetDirectoryName(options.Path)))
        {
            options.Path = Path.Combine(Directory.GetCurrentDirectory(), options.Path);
        }

        this.worker = new(core, unitLogger, options);
        this.options = options;
        this.worker.Start();
    }

    public override void Output(LogOutputParameter param)
    {
        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(new(param));
        }
    }

    public override Task<int> Flush(bool terminate) => this.worker.Flush(terminate);

    private StreamLoggerWorker worker;
    private TOption options;
}
