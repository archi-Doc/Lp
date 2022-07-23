// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Arc.Threading;

namespace Arc.Unit;

internal class ConsoleLoggerWorker : TaskCore
{
    private const int MaxFlush = 1_000;

    public ConsoleLoggerWorker(UnitCore core, SimpleLogFormatterOptions options)
        : base(core, Process)
    {
        this.Formatter = new(options);
    }

    public static async Task Process(object? obj)
    {
        var worker = (ConsoleLoggerWorker)obj!;
        while (worker.Sleep(40))
        {
            await worker.Flush(false);
        }
    }

    public void Add(ConsoleLoggerWork work)
    {
        this.queue.Enqueue(work);
    }

    public async Task<int> Flush(bool terminate)
    {
        var count = 0;
        while (count < MaxFlush && this.queue.TryDequeue(out var work))
        {
            count++;
            Console.WriteLine(this.Formatter.Format(work.Parameter));
        }

        if (terminate)
        {
            this.Terminate();
        }

        return count;
    }

    public int Count => this.queue.Count;

    internal SimpleLogFormatter Formatter { get; set; }

    private ConcurrentQueue<ConsoleLoggerWork> queue = new();
}

internal class ConsoleLoggerWork : ThreadWork
{
    public ConsoleLoggerWork(LogOutputParameter parameter)
    {
        this.Parameter = parameter;
    }

    public LogOutputParameter Parameter { get; }
}
