// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.NetServices;

internal class RemoteBenchBroker
{
    public RemoteBenchBroker(ILogger<RemoteBenchBroker> logger, NetTerminal terminal)
    {
        this.logger = logger;
        this.terminal = terminal;
    }

    public void Register(NetNode? node)
    {
        if (node == null)
        {
            return;
        }

        lock (this.syncObject)
        {
            this.nodes[node] = null;
        }

        this.logger.TryGet()?.Log($"Registered: {node.ToString()}");
    }

    public void Start(int total, int concurrent)
    {
        NetNode[] array;
        lock (this.syncObject)
        {
            array = this.nodes.Keys.ToArray();
            foreach (var x in array)
            {
                this.nodes[x] = null;
            }
        }

        Task[] tasks = new Task[array.Length];
        for (var i = 0; i < array.Length; i++)
        {
            var node = Volatile.Read(ref array[i]);
            tasks[i] = Task.Run(() => StartNode(node));
        }

        async void StartNode(NetNode node)
        {
            this.logger.TryGet()?.Log($"StartNode: {node.ToString()}");
            using (var connection = await this.terminal.Connect(node))
            {
                if (connection is null)
                {
                    return;
                }

                var service = connection.GetService<IBenchmarkService>();
                var result = await service.Start(total, concurrent);
                if (result == NetResult.Success)
                {
                    this.logger.TryGet()?.Log($"Start: {node}");
                }
                else
                {
                    this.logger.TryGet()?.Log($"Unregistered: {node}");
                    lock (this.syncObject)
                    {
                        this.nodes.Remove(node);
                    }
                }
            }
        }
    }

    public void Report(NetNode? node, RemoteBenchRecord record)
    {
        if (node == null)
        {
            return;
        }

        RemoteBenchRecord[] records;
        lock (this.syncObject)
        {
            this.nodes[node] = record;

            if (this.nodes.Values.Any(x => x == null))
            {
                return;
            }

            records = this.nodes.Values.ToArray() as RemoteBenchRecord[];
        }

        long successCount = 0;
        long failureCount = 0;
        long elapsedMilliseconds = 0;
        long countPerSecond = 0;
        long averageLatency = 0;
        foreach (var x in records)
        {
            successCount += x.SuccessCount;
            failureCount += x.FailureCount;
            elapsedMilliseconds += x.ElapsedMilliseconds;
            countPerSecond += x.CountPerSecond;
            averageLatency += x.AverageLatency;
        }

        var count = records.Length;
        elapsedMilliseconds /= count;
        averageLatency /= count;

        this.logger.TryGet()?.Log($"{records.Length} Records:");
        foreach (var x in records)
        {
            this.logger.TryGet()?.Log(x.ToString());
        }

        if (records.Length > 1)
        {
            this.logger.TryGet()?.Log($"Total: Success/Failure {successCount}/{failureCount}, {elapsedMilliseconds} ms, {countPerSecond} c/s, latency {averageLatency} ms");
        }
    }

    private ILogger logger;
    private NetTerminal terminal;

    private object syncObject = new();
    private Dictionary<NetNode, RemoteBenchRecord?> nodes = new();
}
