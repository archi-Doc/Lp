// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
                // transmissionContext.SendAndForget(BitConverter.ToUInt64(hash.HashFinal()));
                stream.SendAndDispose(BitConverter.ToUInt64(hash.HashFinal()));
                break;
            }
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
