// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace RemoteDataServer;

public class RemoteDataBroker : IRemoteData
{
    public RemoteDataBroker(RemoteData broker)
    {
        this.broker = broker;
    }

    private readonly RemoteData broker;

    NetTask<ReceiveStream?> IRemoteData.Get(string identifier)
        => this.broker.Get(identifier);

    NetTask<SendStreamAndReceive<NetResult>?> IRemoteData.Put(string identifier, long maxLength)
        => this.broker.Put(identifier, maxLength);
}
