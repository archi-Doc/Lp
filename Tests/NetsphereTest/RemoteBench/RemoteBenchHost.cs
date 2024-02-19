﻿using Netsphere.Crypto;
using Netsphere.Server;

namespace LP.NetServices;

[NetServiceObject]
public class RemoteBenchHostImpl : IRemoteBenchHost
{
    public RemoteBenchHostImpl()
    {
    }

    public async NetTask<NetResult> Register()
    {
        return NetResult.NoNetService;
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

    public async NetTask<ulong> GetHash(byte[] data)
    {
        return 0;
    }

    public async NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength)
    {
        return default;
    }

    public async NetTask<bool> ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {
        if (token is null ||
           !TransmissionContext.Current.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return false;
        }

        return true;
    }

    public async NetTask<bool> UpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {
        if (!TransmissionContext.Current.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return false;
        }

        return true;
    }
}