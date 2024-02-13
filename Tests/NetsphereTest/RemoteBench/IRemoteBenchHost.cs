// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LP.NetServices;

[NetServiceInterface]
public partial interface IRemoteBenchHost : INetService
{
    public NetTask<byte[]?> Pingpong(byte[] data);

    public NetTask<ulong> GetHash(byte[] data);

    public NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength);

    public NetTask Report(RemoteBenchRecord record);
}
