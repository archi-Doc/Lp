// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Arc.Threading;
using static Arc.Unit.ConsoleLoggerWorker;

namespace Arc.Unit;

internal class FileLoggerWorker : TaskCore
{
    private const int MaxFlush = 1000;

    public FileLoggerWorker(string path, SimpleLogFormatterOptions options)
        : base(ThreadCore.Root, Process)
    {
        this.path = path;
        this.formatter = new(options);
    }

    public static async Task Process(object? obj)
    {
        var worker = (FileLoggerWorker)obj!;
        while (worker.Sleep(1000))
        {
            worker.Flush();
        }
    }

    public void Add(FileLoggerWork work)
    {
        this.queue.Enqueue(work);
    }

    public void Flush()
    {
        if (this.Count == 0)
        {
            return;
        }

        StringBuilder sb = new();
        lock (this.flushSync)
        {
            var i = 0;
            while (i++ < MaxFlush && this.queue.TryDequeue(out var work))
            {
                this.formatter.Format(sb, work.Parameter);
                sb.Append(Environment.NewLine);
            }

            File.AppendAllText(this.path, sb.ToString());
        }
    }

    public int Count => this.queue.Count;

    private string path;
    private SimpleLogFormatter formatter;
    private ConcurrentQueue<FileLoggerWork> queue = new();
    private object flushSync = new();
}

internal class FileLoggerWork
{
    public FileLoggerWork(LogOutputParameter parameter)
    {
        this.Parameter = parameter;
    }

    public LogOutputParameter Parameter { get; }
}
