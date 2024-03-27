// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using LP.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace LP.NetServices;

public class RemoteBenchControl
{
    public RemoteBenchControl(IServiceProvider serviceProvider, ILogger<RemoteBenchControl> logger, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.netTerminal = netTerminal;
        this.fileLogger = serviceProvider.GetService<FileLogger<NetsphereLoggerOptions>>();
    }

    private readonly ILogger logger;
    private readonly NetTerminal netTerminal;
    private readonly FileLogger<NetsphereLoggerOptions>? fileLogger;
    private readonly SingleTask singleTask = new();

    private readonly object syncObject = new();
    private HashSet<ClientConnection> connections = new();
    private Dictionary<ClientConnection, RemoteBenchRecord?> records = new();

    public void Register(ClientConnection clientConnection)
    {
        bool result;
        lock (this.syncObject)
        {
            result = this.connections.Add(clientConnection);
        }

        this.logger.TryGet()?.Log($"Registered({result}): {clientConnection.ToString()}");
    }

    public void Start(Subcommands.RemoteBenchOptions options)
    {
        ClientConnection[] array;
        lock (this.syncObject)
        {
            this.records.Clear();
            array = this.connections.ToArray();
            this.connections.Clear();
            foreach (var x in array)
            {
                this.records[x] = default;
            }
        }

        Task[] tasks = new Task[array.Length];
        for (var i = 0; i < array.Length; i++)
        {
            var c = array[i];
            tasks[i] = Task.Run(() => Process(c));
        }

        async Task Process(ClientConnection clientConnection)
        {
            var service = clientConnection.GetService<IRemoteBenchRunner>();
            var result = await service.Start(options.Total, options.Concurrent, default, default);
            if (result == NetResult.Success)
            {
                this.logger.TryGet()?.Log($"Start: {clientConnection}");
            }
            else
            {
                this.logger.TryGet()?.Log($"Unregistered: {clientConnection}");
            }
        }

        this.singleTask.TryRun(async () =>
        {
            var sw = Stopwatch.StartNew();
            while (await ThreadCore.Root.Delay(1_000))
            {
                if (this.fileLogger is not null)
                {// Reset
                    this.fileLogger.DeleteAllLogs();
                }

                lock (this.syncObject)
                {
                    if (this.records.Values.Any(x => x is null))
                    {// Incomplete
                        if (sw.Elapsed < TimeSpan.FromMinutes(1))
                        {
                            continue;
                        }
                    }

                    var count = 0;
                    long successCount = 0;
                    long failureCount = 0;
                    long elapsedMilliseconds = 0;
                    long countPerSecond = 0;
                    long averageLatency = 0;
                    foreach (var x in this.records)
                    {
                        if (x.Value is not null)
                        {
                            count++;
                            successCount += x.Value.SuccessCount;
                            failureCount += x.Value.FailureCount;
                            elapsedMilliseconds += x.Value.ElapsedMilliseconds;
                            countPerSecond += x.Value.CountPerSecond;
                            averageLatency += x.Value.AverageLatency;
                        }
                    }

                    if (count == 0)
                    {
                        this.logger.TryGet()?.Log($"No record");
                    }
                    else
                    {
                        elapsedMilliseconds /= count;
                        averageLatency /= count;

                        this.logger.TryGet()?.Log($"{count} Records:");
                        this.logger.TryGet()?.Log($"Total: Success/Failure {successCount}/{failureCount}, {elapsedMilliseconds} ms, {countPerSecond} c/s, latency {averageLatency} ms");
                    }

                    break;
                }
            }

            // Send
            await this.SendLog(options);
        });
    }

    public void Report(ClientConnection clientConnection, RemoteBenchRecord record)
    {
        lock (this.syncObject)
        {
            this.records[clientConnection] = record;
        }
    }

    private async Task SendLog(Subcommands.RemoteBenchOptions options)
    {
        if (this.fileLogger is null ||
            string.IsNullOrEmpty(options.NetNode) ||
            string.IsNullOrEmpty(options.RemotePrivateKey))
        {
            return;
        }

        var r = await NetHelper.TryGetStreamService<IRemoteData>(this.netTerminal, options.NetNode, options.RemotePrivateKey, 100_000_000);
        if (r.Connection is null ||
            r.Service is null)
        {
            return;
        }

        await this.fileLogger.Flush(false);
        var path = this.fileLogger.GetCurrentPath();

        try
        {
            using var fileStream = File.OpenRead(path);
            var sendStream = await r.Service.Put("RemoteBenchServer.txt", fileStream.Length);
            if (sendStream is not null)
            {
                var r3 = await NetHelper.StreamToSendStream(fileStream, sendStream);
            }
        }
        catch
        {
        }
    }
}
