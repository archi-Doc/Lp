// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

[NetServiceInterface]
public interface IDomainService : INetServiceWithOwner
{
    NetTask<NetResult> RegisterNode(NodeProof nodeProof);

    Task<NetResultAndValue<NetNode>> GetNode(SignaturePublicKey publicKey);
}
