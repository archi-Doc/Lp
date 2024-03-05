// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[NetServiceInterface]
public interface IRemoteData : INetService
{
    NetTask<ReceiveStream?> Get(string identifier);

    NetTask<SendStream?> Put(string identifier, long maxLength);

    NetTask<SendStream?> Put2(string identifier, ulong hash, long maxLength);
}
