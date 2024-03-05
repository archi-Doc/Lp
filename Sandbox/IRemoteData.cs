// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[NetServiceInterface]
public interface IRemoteData : INetService
{
    NetTask<ReceiveStream> Get(string identifier);

    NetTask<SendStream> Put(long maxLength);
}
