// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Netsphere.Crypto;

namespace Lp.NetServices;

[NetObject]
public class RemoteBenchHostAgent : IRemoteBenchHost, IRemoteBenchService
{
    public RemoteBenchHostAgent(RemoteBenchControl broker)
    {
        this.broker = broker;
    }

    private readonly RemoteBenchControl broker;

    public async Task Report(RemoteBenchRecord record)
    {
        var context = TransmissionContext.Current;
        if (context.ServerConnection.BidirectionalConnection is { } connection)
        {
            this.broker.Report(connection, record);
            context.Result = NetResult.Success;
        }
        else
        {
            context.Result = NetResult.InvalidOperation;
        }
    }

    public async Task<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    async Task<ulong> IRemoteBenchService.GetHash(byte[] data)
    {
        return FarmHash.Hash64(data);
    }

    public async Task<SendStreamAndReceive<ulong>?> GetHash(long maxLength)
    {
        var transmissionContext = TransmissionContext.Current;
        var stream = transmissionContext.GetReceiveStream<ulong>();

        var buffer = ArrayPool<byte>.Shared.Rent(100_000);
        var hash = new XxHash3();
        try
        {
            while (true)
            {
                var r = await stream.Receive(buffer.AsMemory(0, 100_000));
                if (r.Result == NetResult.Success ||
                    r.Result == NetResult.Completed)
                {
                    hash.Append(buffer.AsSpan(0, r.Written));
                }
                else
                {
                    break;
                }

                if (r.Result == NetResult.Completed)
                {
                    // transmissionContext.SendAndForget(BitConverter.ToUInt64(hash.HashFinal()));
                    stream.SendAndDispose(hash.GetCurrentHashAsUInt64());
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return default;
    }

    public async Task<NetResult> ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {
        var context = TransmissionContext.Current;
        if (token is null ||
           !context.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return NetResult.NotAuthenticated;
        }

        var clientConnection = context.ServerConnection.PrepareBidirectionalConnection();
        this.broker.Register(clientConnection);

        return NetResult.Success;
    }

    public async Task<NetResult> UpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {
        if (!TransmissionContext.Current.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return NetResult.NotAuthenticated;
        }

        return NetResult.Success;
    }
}
