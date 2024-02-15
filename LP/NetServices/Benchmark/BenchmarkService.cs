// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Server;

namespace LP.NetServices;

[NetServiceObject]
[NetServiceFilter<TestOnlyFilter>]
internal class BenchmarkServiceImpl : IBenchmarkService
{
    public BenchmarkServiceImpl(ILogger<IBenchmarkService> logger, RemoteBenchBroker remoteBenchBroker)
    {
        this.logger = logger;
        this.remoteBenchBroker = remoteBenchBroker;
    }

    public async NetTask<NetResult> Register()
    {
        this.remoteBenchBroker.Register(TransmissionContext.Current.ServerConnection.DestinationNode);
        return NetResult.Success;
    }

    public async NetTask<NetResult> Start(int total, int concurrent)
    {
        return NetResult.NoNetService;
    }

    public async NetTask Report(RemoteBenchRecord record)
    {
        this.remoteBenchBroker.Report(TransmissionContext.Current.ServerConnection.DestinationNode, record);
    }

    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    public async NetTask<ulong> GetHash(byte[] data)
    {
        return FarmHash.Hash64(data);
    }

    public async NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength)
    {
        var transmissionContext = TransmissionContext.Current;
        var stream = transmissionContext.ReceiveStream;

        var buffer = new byte[100_000];
        var hash = new FarmHash();
        hash.HashInitialize();
        long total = 0;

        while (true)
        {
            var r = await stream.Receive(buffer);
            if (r.Result == NetResult.Success ||
                r.Result == NetResult.Completed)
            {
                hash.HashUpdate(buffer.AsMemory(0, r.Written).Span);
                total += r.Written;
            }
            else
            {
                break;
            }

            if (r.Result == NetResult.Completed)
            {
                transmissionContext.SendAndForget(BitConverter.ToUInt64(hash.HashFinal()));
                break;
            }
        }

        return default;
    }

    private ILogger logger;
    private RemoteBenchBroker remoteBenchBroker;
}
