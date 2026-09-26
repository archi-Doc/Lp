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
        this.fileLogger = serviceProvider.GetService<FileLogOutput<NetsphereLoggerOptions>>();
    }

    private readonly ILogger logger;
    private readonly NetTerminal netTerminal;
    private readonly IFileLogOutput? fileLogger;
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

        this.logger.GetWriter()?.Write($"Registered({result}): {clientConnection.ToString()}");
    }

    /// <summary>
    /// Starts the registered runners and aggregates their records.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task that aggregates the records, or <see langword="null"/> if the aggregation is already running.</returns>
    public Task? Start(Subcommands.RemoteBenchOptions options, CancellationToken cancellationToken)
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

        for (var i = 0; i < array.Length; i++)
        {
            _ = Process(i, array[i]);
        }

        async Task Process(int index, ClientConnection clientConnection)
        {
            var service = clientConnection.GetService<IRemoteBenchRunner>();
            var remoteNode = index == 0 ? null : options.Node;
            var remotePrivateKey = index == 0 ? null : options.RemotePrivateKey;
            var result = await service.Start(options.Total, options.Concurrent, remoteNode, remotePrivateKey);
            if (result == NetResult.Success)
            {
                this.logger.GetWriter()?.Write($"Start: {clientConnection}");
            }
            else
            {
                lock (this.syncObject)
                {// Do not wait for a record that will never be reported.
                    this.records.Remove(clientConnection);
                }

                this.logger.GetWriter()?.Write($"Unregistered: {clientConnection}");
            }
        }

        if (this.fileLogger is not null)
        {// Reset
            this.fileLogger.DeleteAllLogs();
        }

        return this.singleTask.TryRun(async () =>
        {
            var sw = Stopwatch.StartNew();
            while (await Task.TryDelay(1_000, cancellationToken))
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
                        this.logger.GetWriter()?.Write($"No record");
                    }
                    else
                    {
                        elapsedMilliseconds /= count;
                        averageLatency /= count;

                        this.logger.GetWriter()?.Write($"{count} Records:");
                        this.logger.GetWriter()?.Write($"Total: Success/Failure {successCount}/{failureCount}, {elapsedMilliseconds} ms, {countPerSecond} c/s, latency {averageLatency} ms");
                    }

                    break;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {// The command was canceled (it waits for this task).
                return;
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
