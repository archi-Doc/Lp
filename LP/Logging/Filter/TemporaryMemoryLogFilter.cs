// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

public class LogWriterCache<TSource>
{
    public LogWriterCache()
    {
    }

    public ILogWriter? TryGet(LogFilterParameter param)
    {
        return param.LogLevel switch
        {
            LogLevel.Debug => this.debugWriter ??= param.Context.TryGet<TSource>(LogLevel.Debug),
            LogLevel.Information => this.informationWriter ??= param.Context.TryGet<TSource>(LogLevel.Information),
            LogLevel.Warning => this.warningWriter ??= param.Context.TryGet<TSource>(LogLevel.Warning),
            LogLevel.Error => this.errorWriter ??= param.Context.TryGet<TSource>(LogLevel.Error),
            LogLevel.Fatal => this.fatalWriter ??= param.Context.TryGet<TSource>(LogLevel.Fatal),
            _ => default,
        };
    }

    private ILogWriter? debugWriter;
    private ILogWriter? informationWriter;
    private ILogWriter? warningWriter;
    private ILogWriter? errorWriter;
    private ILogWriter? fatalWriter;
}

public class TemporaryMemoryLogFilter : ILogFilter
{
    public TemporaryMemoryLogFilter()
    {
    }

    public ILogWriter? Filter(LogFilterParameter param)
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
