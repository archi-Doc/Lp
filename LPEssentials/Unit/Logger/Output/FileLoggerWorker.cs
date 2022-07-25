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
    private const int LimitLogThreshold = 10_000;

    public FileLoggerWorker(UnitCore core, UnitLogger unitLogger, string fullPath, int maxCapacity, SimpleLogFormatterOptions options)
        : base(core, Process)
    {
        this.logger = unitLogger.GetLogger<FileLoggerWorker>();
        this.formatter = new(options);

        this.maxCapacity = maxCapacity * 1_000_000;
        var idx = fullPath.LastIndexOf('.'); // "TestLog.txt" -> 7
        if (idx >= 0)
        {
            this.basePath = fullPath.Substring(0, idx);
            this.baseExtension = fullPath.Substring(idx);
        }
        else
        {
            this.basePath = fullPath;
            this.baseExtension = string.Empty;
        }

        this.baseFile = Path.GetFileName(this.basePath);
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
                var path = this.GetCurrentPath();
                if (Path.GetDirectoryName(path) is { } directory)
                {
                    Directory.CreateDirectory(directory);
                }

                await File.AppendAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
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
                    this.limitLogCount >= LimitLogThreshold)
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

    private string GetCurrentPath()
        => this.basePath + DateTime.Now.ToString("yyyyMMdd") + this.baseExtension;

    private void LimitLogs()
    {
        var currentPath = this.GetCurrentPath();
        var directory = Path.GetDirectoryName(currentPath);
        var file = Path.GetFileName(currentPath);
        if (directory == null || file == null)
        {
            return;
        }

        long capacity = 0;
        SortedDictionary<string, long> pathToSize = new();
        try
        {
            foreach (var x in Directory.EnumerateFiles(directory, this.baseFile + "*" + this.baseExtension, SearchOption.TopDirectoryOnly))
            {
                if (x.Length == currentPath.Length)
                {
                    try
                    {
                        var size = new FileInfo(x).Length;
                        pathToSize.Add(x, size);
                        capacity += size;
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
            return;
        }

        this.logger?.TryGet()?.Log($"Limit logs {capacity}/{this.maxCapacity} {directory}");
        foreach (var x in pathToSize)
        {
            if (capacity < this.maxCapacity)
            {
                break;
            }

            try
            {
                File.Delete(x.Key);
                this.logger?.TryGet()?.Log($"Deleted: {x.Key}");
            }
            catch
            {
            }

            capacity -= x.Value;
        }
    }

    public int Count => this.queue.Count;

    private ILogger<FileLoggerWorker>? logger;
    private string basePath;
    private string baseFile;
    private string baseExtension;
    private SimpleLogFormatter formatter;
    private ConcurrentQueue<FileLoggerWork> queue = new();
    private SemaphoreSlim semaphore = new(1, 1);
    private DateTime limitLogTime;
    private int limitLogCount = 0;
    private long maxCapacity;
}

internal class FileLoggerWork
{
    public FileLoggerWork(LogOutputParameter parameter)
    {
        this.Parameter = parameter;
    }

    public LogOutputParameter Parameter { get; }
}
