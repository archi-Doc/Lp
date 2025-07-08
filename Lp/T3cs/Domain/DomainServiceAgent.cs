// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

[NetServiceObject]
internal class DomainServiceAgent : IDomainService
{
    private readonly IDomainService domainService; // Underlying service implementation (CreditDomain)

    public DomainServiceAgent(DomainControl domainControl)
    {
        this.domainService = domainControl.domainService;
    }

    public async NetTask<NetResult> Authenticate(OwnerToken token)
    {
        if (this.domainService is null)
        {
            return NetResult.NoNetService;
        }

        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (!token.ValidateAndVerify(serverConnection))
        {
            return NetResult.NotAuthenticated;
        }

        if (token.Credit is null)
        {
            return NetResult.InvalidData;
        }

        return NetResult.Success;
    }

    public NetTask<NetResult> RegisterNode(NodeProof nodeProof)
        => this.domainService.RegisterNode(nodeProof);

    public Task<NetResultAndValue<NetNode>> GetNode(SignaturePublicKey publicKey)
        => this.domainService.GetNode(publicKey);
}
