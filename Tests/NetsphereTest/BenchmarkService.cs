using System.Diagnostics;
using NetsphereTest;

namespace LP.NetServices;

public class RemoteBenchBroker
{
    public RemoteBenchBroker()
    {
    }

    public void Start(int total, int concurrent)
    {
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

    public async Task Process(Terminal terminal, NodeInformation node)
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
                    using (var t = terminal.Create(node))
                    {
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

        var record = new IBenchmarkService.ReportRecord()
        {
            SuccessCount = successCount,
            FailureCount = failureCount,
            Concurrent = concurrent,
            ElapsedMilliseconds = sw.ElapsedMilliseconds,
            CountPerSecond = (int)((successCount + failureCount) * 1000 / sw.ElapsedMilliseconds),
            AverageLatency = (int)(totalLatency / (successCount + failureCount)),
        };

        using (var t = terminal.Create(node))
        {
            var service = t.GetService<IBenchmarkService>();
            await service.Report(record);
        }

        await Console.Out.WriteLineAsync(record.ToString());
    }

    public int Total => this.total;

    public int Concurrent => this.concurrent;

    private AsyncPulseEvent pulseEvent = new();
    private int total;
    private int concurrent;
}

[NetServiceFilter(typeof(TestFilter), Order = 1)]
[NetServiceFilter(typeof(TestFilterB), Order = 1)]
[NetServiceObject]
public class BenchmarkServiceImpl : IBenchmarkService
{
    public BenchmarkServiceImpl(RemoteBenchBroker remoteBenchBroker)
    {
        this.remoteBenchBroker = remoteBenchBroker;
    }

    public async NetTask<NetResult> Register()
    {
        return NetResult.NoNetService;
    }

    public async NetTask<NetResult> Start(int total, int concurrent)
    {
        this.remoteBenchBroker.Start(total, concurrent);
        return NetResult.Success;
    }

    public async NetTask Report(IBenchmarkService.ReportRecord record)
    {
        // await Console.Out.WriteLineAsync(record.ToString());
    }

    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    public async NetTask Send(byte[] data)
    {
    }

    // [NetServiceFilter(typeof(NullFilter))]
    public async NetTask Wait(int millisecondsToWait)
    {
        if (CallContext.Current is not TestCallContext context)
        {
            throw new NetException(NetResult.NoCallContext);
        }

        Console.Write("Wait -> ");
        await Task.Delay(millisecondsToWait);
        Console.WriteLine($"{millisecondsToWait}");

        context.Result = NetResult.NoEncryptedConnection;
    }

    private RemoteBenchBroker remoteBenchBroker;
}

public class TestFilterB : TestFilter
{
    public async Task Invoke(ServerContext context, Func<ServerContext, Task> next)
    {
        await next(context);
    }

    public TestFilterB(NetControl aa)
    {
    }
}

public class TestFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        await invoker(context);
    }
}

public class NullFilter : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        context.Result = NetResult.NoNetService;
    }
}

public class TestFilter2 : IServiceFilter
{
    public async Task Invoke(CallContext context, Func<CallContext, Task> next)
    {
        await next(context);
    }
}
