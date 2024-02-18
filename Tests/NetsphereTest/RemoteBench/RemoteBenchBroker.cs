using System.Diagnostics;
using Arc.Unit;

namespace LP.NetServices;

/*public class RemoteBenchBroker
{
    public RemoteBenchBroker(ILogger<RemoteBenchBroker> logger)
    {
        this.logger = logger;
    }

    public void Start(int total, int concurrent)
    {
        if (total == 0)
        {
            total = 1_000;
        }

        if (concurrent == 0)
        {
            concurrent = 25;
        }

        Volatile.Write(ref this.total, total);
        Volatile.Write(ref this.concurrent, concurrent);
        this.pulseEvent.Pulse();
    }

    public async Task<bool> Wait()
    {
        try
        {
            await this.pulseEvent.WaitAsync(TimeSpan.FromMinutes(10), ThreadCore.Root.CancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task Process(NetTerminal terminal, NetNode node)
    {
        var data = new byte[100];
        int successCount = 0;
        int failureCount = 0;
        long totalLatency = 0;

        var total = this.total;
        var concurrent = this.concurrent;

        // ThreadPool.GetMinThreads(out var workMin, out var ioMin);
        // ThreadPool.SetMinThreads(3000, ioMin);

        var sw = Stopwatch.StartNew();
        var array = new Task[this.concurrent];
        for (int i = 0; i < this.concurrent; i++)
        {
            array[i] = Task.Run(async () =>
            {
                for (var j = 0; j < (total / concurrent); j++)
                {
                    var sw2 = new Stopwatch();
                    using (var t = await terminal.TryConnect(node, Connection.ConnectMode.NoReuse))
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

        using (var t = await terminal.TryConnect(node, Connection.ConnectMode.NoReuse))
        {
            if (t is null)
            {
                return;
            }

            var service = t.GetService<IBenchmarkService>();
            await service.Report(record);
        }

        this.logger.TryGet()?.Log(record.ToString());
    }

    public int Total => this.total;

    public int Concurrent => this.concurrent;

    private readonly AsyncPulseEvent pulseEvent = new();
    private readonly ILogger logger;
    private int total;
    private int concurrent;
}*/
