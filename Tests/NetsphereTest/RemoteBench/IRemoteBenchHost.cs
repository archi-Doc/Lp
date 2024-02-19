// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Net;

namespace LP.NetServices;

[NetServiceInterface]
public interface IRemoteBenchHost : INetService, INetServiceBidirectional
{
    NetTask<byte[]?> Pingpong(byte[] data);

    NetTask<ulong> GetHash(byte[] data);

    NetTask<SendStreamAndReceive<ulong>?> GetHash(long maxLength);

    NetTask<NetResult> Register(CertificateToken<ConnectionAgreement> token);

    NetTask Report(RemoteBenchRecord record);
}
