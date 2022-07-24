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
    private const int MaxFlush = 10_000;
    private const int LimitLogThreshold = 10;

    public FileLoggerWorker(UnitCore core, UnitLogger unitLogger, string path, SimpleLogFormatterOptions options)
        : base(core, Process)
    {
        this.logger = unitLogger.GetLogger<FileLoggerWorker>();
        this.path = path;
        this.formatter = new(options);
    }

    public static async Task Process(object? obj)
    {
        var worker = (FileLoggerWorker)obj!;
        while (worker.Sleep(1000))
        {
            await worker.Flush(false).ConfigureAwait(false);
        }

        await worker.Flush(false);
    }

    public void Add(FileLoggerWork work)
    {
        this.queue.Enqueue(work);
    }

    public async Task<int> Flush(bool terminate)
    {
        this.logger?.TryGet(LogLevel.Information)?.Log("Flush");
        await this.semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            StringBuilder sb = new();
            var count = 0;
            while (count < MaxFlush && this.queue.TryDequeue(out var work))
            {
                count++;
                this.formatter.Format(sb, work.Parameter);
                sb.Append(Environment.NewLine);
            }

            if (count != 0)
            {
                await File.AppendAllTextAsync(this.path, sb.ToString()).ConfigureAwait(false);
            }

            if (terminate)
            {
                this.Terminate();
            }
            else
            {// Limit log capacity
                this.limitLogCount += count;
                var now = DateTime.UtcNow;
                if (now - this.limitLogTime > TimeSpan.FromMinutes(10) ||
                    this.limitLogCount > LimitLogThreshold)
                {
                    this.limitLogTime = now;
                    this.limitLogCount = 0;

                    this.LimitLogs();
                }
            }

            return count;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public void LimitLogs()
    {
    }

    public int Count => this.queue.Count;

    private ILogger<FileLoggerWorker> logger;
    private string path;
    private SimpleLogFormatter formatter;
    private ConcurrentQueue<FileLoggerWork> queue = new();
    private SemaphoreSlim semaphore = new(1, 1);
    private DateTime limitLogTime;
    private int limitLogCount = 0;
}

internal class FileLoggerWork
{
    public FileLoggerWork(LogOutputParameter parameter)
    {
        this.Parameter = parameter;
    }

    public LogOutputParameter Parameter { get; }
}
