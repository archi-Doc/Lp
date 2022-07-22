// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using Arc.Threading;

namespace Arc.Unit;

internal class ConsoleLoggerWorker : ThreadWorker<ConsoleLoggerWork>
{
    public ConsoleLoggerWorker(SimpleLogFormatterOptions options)
        : base(ThreadCore.Root, Process)
    {
        this.formatter = new(options);
        this.Thread.IsBackground = true;
    }

    public static AbortOrComplete Process(ThreadWorker<ConsoleLoggerWork> worker, ConsoleLoggerWork work)
    {
        var w = (ConsoleLoggerWorker)worker;
        Console.WriteLine(w.formatter.Format(work.Parameter));
        return AbortOrComplete.Complete;
    }

    private SimpleLogFormatter formatter;
}

internal class ConsoleLoggerWork : ThreadWork
{
    public ConsoleLoggerWork(LogOutputParameter parameter)
    {
        this.Parameter = parameter;
    }

    public LogOutputParameter Parameter { get; }
}
