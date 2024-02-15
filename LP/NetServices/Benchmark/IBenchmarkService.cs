// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Tinyhand;

namespace LP.NetServices;

[NetServiceInterface]
public interface IBenchmarkService : INetService
{
    public NetTask<byte[]?> Pingpong(byte[] data);

    public NetTask<ulong> GetHash(byte[] data);

    public NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength);

    public NetTask<NetResult> Register();

    public NetTask<NetResult> Start(int total, int concurrent);

    public NetTask Report(RemoteBenchRecord record);
}
