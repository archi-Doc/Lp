// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LP.NetServices;

[NetServiceInterface]
public interface IRemoteBenchHost : INetService
{
    NetTask<byte[]?> Pingpong(byte[] data);

    NetTask<ulong> GetHash(byte[] data);

    NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength);

    NetTask<NetResult> Register();

    NetTask Report(RemoteBenchRecord record);
}
