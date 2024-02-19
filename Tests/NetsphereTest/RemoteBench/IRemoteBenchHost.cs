// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Net;

namespace LP.NetServices;

[NetServiceInterface]
public interface IRemoteBenchHost : INetService, INetServiceBidirectional, INetServiceAgreement
{
    NetTask<byte[]?> Pingpong(byte[] data);

    NetTask<ulong> GetHash(byte[] data);

    NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength);

    NetTask Report(RemoteBenchRecord record);
}
