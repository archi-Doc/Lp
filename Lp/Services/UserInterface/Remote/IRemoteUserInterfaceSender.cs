// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.NetServices;

[NetService]
public interface IRemoteUserInterfaceSender : INetService
{
    Task<NetResultAndValue<string>> ConnectBidirectionally(CertificateToken<ConnectionAgreement> token);

    Task<NetResult> Send(long id, string message);
}
