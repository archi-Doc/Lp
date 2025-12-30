// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Lp.Data;
using Lp.Logging;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Interfaces;

namespace Lp.NetServices;

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
    private readonly IFileLogger? fileLogger;
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
            tasks[i] = Task.Run(() => Process(i, c));
        }

        async Task Process(int index, ClientConnection clientConnection)
        {
            var service = clientConnection.GetService<IRemoteBenchRunner>();
            var remoteNode = index == 0 ? null : options.Node;
            var remotePrivateKey = index == 0 ? null : options.RemotePrivateKey;
            var result = await service.Start(options.Total, options.Concurrent, remoteNode, remotePrivateKey);
            if (result == NetResult.Success)
            {
                this.logger.TryGet()?.Log($"Start: {clientConnection}");
            }
            else
            {
                this.logger.TryGet()?.Log($"Unregistered: {clientConnection}");
            }
        }

        if (this.fileLogger is not null)
        {// Reset
            this.fileLogger.DeleteAllLogs();
        }

        this.singleTask.TryRun(async () =>
        {
            var sw = Stopwatch.StartNew();
            while (await ThreadCore.Root.Delay(1_000))
            {
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
            await RemoteDataHelper.SendLog(this.netTerminal, this.fileLogger, options.Node, options.RemotePrivateKey, "RemoteBench.Server.txt");
        });
    }

    public void Report(ClientConnection clientConnection, RemoteBenchRecord record)
    {
        lock (this.syncObject)
        {
            this.records[clientConnection] = record;
        }
    }
}
