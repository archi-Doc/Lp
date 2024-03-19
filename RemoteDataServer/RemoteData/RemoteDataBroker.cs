// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace RemoteDataServer;

[NetServiceObject]
public class RemoteDataBroker : IRemoteData
{
    public RemoteDataBroker(RemoteData broker)
    {
        this.broker = broker;
    }

    private readonly RemoteData broker;

    NetTask<NetResult> INetServiceAgreement.UpdateAgreement(CertificateToken<ConnectionAgreement> token)
        => this.broker.UpdateAgreement(token);

    NetTask<ReceiveStream?> IRemoteData.Get(string identifier)
        => this.broker.Get(identifier);

    NetTask<SendStreamAndReceive<NetResult>?> IRemoteData.Put(string identifier, long maxLength)
        => this.broker.Put(identifier, maxLength);
}
