// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

[NetServiceInterface]
public interface IDomainService : INetService
{
    NetTask<NetResult> RegisterNode(NodeProof nodeProof);

    NetTask<NetResultValue<NetNode>> GetNode(SignaturePublicKey publicKey);
}
