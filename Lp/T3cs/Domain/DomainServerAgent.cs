// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

/*[NetServiceObject]
internal class DomainServiceAgent : IDomainService
{
    private readonly IDomainService domainService; // Underlying service implementation (DomainServer)

    public DomainServiceAgent(DomainControl domainControl)
    {
        this.domainService = domainControl.DomainServer;
    }

    public NetTask<NetResult> Authenticate(OwnerToken token)
        => this.domainService.Authenticate(token);

    public NetTask<NetResult> RegisterNode(NodeProof nodeProof)
        => this.domainService.RegisterNode(nodeProof);

    public Task<NetResultAndValue<NetNode>> GetNode(SignaturePublicKey publicKey)
        => this.domainService.GetNode(publicKey);
}*/
