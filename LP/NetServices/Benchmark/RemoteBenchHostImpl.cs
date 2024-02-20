using Netsphere.Crypto;

namespace LP.NetServices;

[NetServiceObject]
public class RemoteBenchHostImpl : IRemoteBenchHost, IRemoteBenchService
{
    public RemoteBenchHostImpl()
    {
    }

    public async NetTask<NetResult> Start(int total, int concurrent)
    {
        return NetResult.Success;
    }

    public async NetTask Report(RemoteBenchRecord record)
    {
    }

    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    async NetTask<ulong> IRemoteBenchService.GetHash(byte[] data)
    {
        return 0;
    }

    public async NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength)
    {
        return default;
    }

    public async NetTask<NetResult> ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {
        var context = TransmissionContext.Current;
        if (token is null ||
           !context.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return NetResult.NotAuthorized;
        }

        var clientConnection = context.ServerConnection.PrepareBidirectionalConnection();
        var service = clientConnection.GetService<IRemoteBenchRunner>();
        if (service is not null)
        {
            // var result = await service.Start(10_000, 20);
        }

        return NetResult.Success;
    }

    public async NetTask<NetResult> UpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {
        if (!TransmissionContext.Current.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return NetResult.NotAuthorized;
        }

        return NetResult.Success;
    }
}
