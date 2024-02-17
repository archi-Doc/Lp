﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;
using Netsphere.Server;

namespace LP.NetServices;

[NetServiceObject]
public class RemoteBenchRunnerImpl : IRemoteBenchRunner, INetServiceHandler
{
    public RemoteBenchRunnerImpl(ILogger<RemoteBenchRunnerImpl> logger, RemoteBenchBroker remoteBenchBroker, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.remoteBenchBroker = remoteBenchBroker;
        this.netTerminal = netTerminal;
    }

    #region FieldAndProperty

    private readonly ILogger logger;
    private readonly RemoteBenchBroker remoteBenchBroker;
    private readonly NetTerminal netTerminal;

    #endregion

    public async NetTask<NetResult> Start(int total, int concurrent)
    {
        if (total == 0)
        {
            total = 1_000;
        }

        if (concurrent == 0)
        {
            concurrent = 25;
        }

        var transmissionContext = TransmissionContext.Current;
        this.logger.TryGet()?.Log($"Benchmark {transmissionContext.ServerConnection.DestinationNode.ToString()}, Total/Concurrent: {total}/{concurrent}");

        var serverConnection = transmissionContext.ServerConnection;
        var connectionContext = serverConnection.GetContext();
        var clientConnection = (ClientConnection)serverConnection.GetBidirectional();

        var data = new byte[100];
        int successCount = 0;
        int failureCount = 0;
        long totalLatency = 0;

        // ThreadPool.GetMinThreads(out var workMin, out var ioMin);
        // ThreadPool.SetMinThreads(3000, ioMin);

        var sw = Stopwatch.StartNew();
        var array = new Task[concurrent];
        for (int i = 0; i < concurrent; i++)
        {
            array[i] = Task.Run(async () =>
            {
                for (var j = 0; j < (total / concurrent); j++)
                {
                    var sw2 = new Stopwatch();
                    using (var t = await this.netTerminal.TryConnect(transmissionContext.ServerConnection.DestinationNode, Connection.ConnectMode.NoReuse))
                    {
                        if (t is null)
                        {
                            return;
                        }

                        var service = t.GetService<IBenchmarkService>();
                        sw2.Restart();

                        var response = await service.Pingpong(data).ResponseAsync; // response.Result.IsSuccess is EVIL
                        if (response.IsSuccess)
                        {
                            Interlocked.Increment(ref successCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref failureCount);
                        }

                        sw2.Stop();
                        Interlocked.Add(ref totalLatency, sw2.ElapsedMilliseconds);
                    }
                }
            });
        }

        await Task.WhenAll(array);

        // ThreadPool.SetMinThreads(workMin, ioMin);

        sw.Stop();

        var totalCount = successCount + failureCount;
        if (totalCount == 0)
        {
            totalCount = 1;
        }

        var record = new RemoteBenchRecord()
        {
            SuccessCount = successCount,
            FailureCount = failureCount,
            Concurrent = concurrent,
            ElapsedMilliseconds = sw.ElapsedMilliseconds,
            CountPerSecond = (int)(totalCount * 1000 / sw.ElapsedMilliseconds),
            AverageLatency = (int)(totalLatency / totalCount),
        };

        var service =clientConnection.GetService<IBenchmarkService>();
        await service.Report(record);

        this.logger.TryGet()?.Log(record.ToString());

        return NetResult.Success;
    }

    void INetServiceHandler.OnConnected()
    {
    }

    void INetServiceHandler.OnDisconnected()
    {
    }
}

[NetServiceInterface]
public partial interface IRemoteBenchRunner : INetService
{
    NetTask<NetResult> Start(int total, int concurrent);
}

public interface INetServiceHandler
{
    void OnConnected();

    void OnDisconnected();
}

public interface IBidirectionalService
{
    ClientConnection ClientConnection { get; }
}