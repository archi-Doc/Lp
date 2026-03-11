// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Logging;

public class LogWriterCache<TSource>
{
    public LogWriterCache()
    {
    }

    public LogWriter? TryGet(LogFilterParameter param)
    {
        return param.LogLevel switch
        {
            LogLevel.Debug => this.debugWriter ??= param.LogService.GetWriter<TSource>(LogLevel.Debug),
            LogLevel.Information => this.informationWriter ??= param.LogService.GetWriter<TSource>(LogLevel.Information),
            LogLevel.Warning => this.warningWriter ??= param.LogService.GetWriter<TSource>(LogLevel.Warning),
            LogLevel.Error => this.errorWriter ??= param.LogService.GetWriter<TSource>(LogLevel.Error),
            LogLevel.Fatal => this.fatalWriter ??= param.LogService.GetWriter<TSource>(LogLevel.Fatal),
            _ => default,
        };
    }

    private LogWriter? debugWriter;
    private LogWriter? informationWriter;
    private LogWriter? warningWriter;
    private LogWriter? errorWriter;
    private LogWriter? fatalWriter;
}

public class TemporaryMemoryLogFilter : ILogFilter
{
    public TemporaryMemoryLogFilter()
    {
    }

    public LogWriter? Filter(LogFilterParameter param)
    {
        if (this.Enabled)
        {
            return this.cache.TryGet(param);
        }

        return param.OriginalLogger;
    }

    public bool Enabled { get; set; }

    private LogWriterCache<MemoryLogger> cache = new();
}
