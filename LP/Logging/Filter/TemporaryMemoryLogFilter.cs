// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

internal class TemporaryMemoryLogFilter : ILogFilter
{
    public TemporaryMemoryLogFilter()
    {
    }

    public ILogWriter? Filter(LogFilterParameter param)
    {
        if (true)
        {
            return this.logWriter ??= param.Context.TryGet<MemoryLogger>(param.LogLevel);//
        }

        return param.OriginalLogger;
    }

    private ILogWriter? logWriter;
}
