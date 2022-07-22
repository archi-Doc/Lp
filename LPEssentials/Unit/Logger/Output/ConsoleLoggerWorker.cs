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

    public ConsoleLoggerWorker(SimpleLogFormatterOptions options)
        : base(ThreadCore.Root, Process)
    {
        this.formatter = new(options);
    }

    public static async Task Process(object? obj)
    {
        var worker = (ConsoleLoggerWorker)obj!;
        while (worker.Sleep(30))
        {
            await worker.Flush();
        }
    }

    public void Add(ConsoleLoggerWork work)
    {
        this.queue.Enqueue(work);
    }

    public async Task<int> Flush()
    {
        var count = 0;
        while (count++ < MaxFlush && this.queue.TryDequeue(out var work))
        {
            Console.WriteLine(this.formatter.Format(work.Parameter));
        }

        return count;
    }

    public int Count => this.queue.Count;

    private SimpleLogFormatter formatter;
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
