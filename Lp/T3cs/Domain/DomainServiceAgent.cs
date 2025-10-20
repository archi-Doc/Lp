// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

[NetServiceObject]
internal class DomainServiceAgent : IDomainService
{
    private readonly DomainControl domainControl;

    public DomainServiceAgent(DomainControl domainControl)
    {
        this.domainControl = domainControl;
    }

    Task<NetResultAndValue<DomainOverview>> IDomainService.GetOverview(ulong domainHash)
    {
        var domainService = this.domainControl.GetDomainService(domainHash);
        if (domainService is null)
        {
            return Task.FromResult<NetResultAndValue<DomainOverview>>(new(NetResult.NotFound));
        }

        return Task.FromResult<NetResultAndValue<DomainOverview>>(new(domainService.GetOverview()));
    }
}

/*[NetServiceObject]
internal class DomainServiceAgent : IDomainService
{
    private readonly DomainServer domainServer; // Underlying service implementation (DomainServer)

    public DomainServiceAgent(DomainControl domainControl)
    {
        this.domainServer = domainControl.DomainServer;
    }

    async NetTask<NetResult> INetServiceWithOwner.Authenticate(OwnerToken token)
    {
        if (!this.domainServer.IsActive)
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

    async NetTask<NetResult> IDomainService.RegisterNode(NodeProof nodeProof)
    {
        if (!this.domainServer.IsActive)
        {
            return NetResult.NoNetService;
        }

        if (!nodeProof.NetNode.Validate() ||
            !nodeProof.NetNode.Address.IsValidIpv4AndIpv6)
        {
            return NetResult.InvalidData;
        }

        if (!nodeProof.ValidateAndVerify())
        {
            return NetResult.InvalidData;
        }

        if (this.domainServer.CreditDomain.TryGetNetNode(nodeProof.PublicKey, out _))
        {// Check whether the PublicKey is registered in CreditDomain.
            nodeProof.IsAuthorized = true;
        }
        else
        {
            nodeProof.IsAuthorized = false;
        }

        using (this.domainServer.Nodes.LockObject.EnterScope())
        {
            if (this.domainServer.Nodes.PublicKeyChain.FindFirst(nodeProof.PublicKey) is { } first)
            {// Found
                if (first.PriorityMics >= nodeProof.PriorityMics)
                {// Existing proof is newer or equal
                    return NetResult.Success;
                }
                else
                {
                    first.Goshujin = default; // Remove the existing proof.
                }
            }

            this.domainServer.Nodes.Add(nodeProof);

            while (this.domainServer.Nodes.Count > DomainServer.MaxNodeCount)
            {// Remove the oldest NodeProofs if the count exceeds the maximum.
                if (this.domainServer.Nodes.PriorityMicsChain.First is { } node)
                {
                    node.Goshujin = default;
                }
            }
        }

        return NetResult.Success;
    }

    async Task<NetResultAndValue<NetNode>> IDomainService.GetNode(SignaturePublicKey publicKey)
    {
        if (!this.domainServer.IsActive)
        {
            return new(NetResult.NoNetService);
        }

        using (this.domainServer.Nodes.LockObject.EnterScope())
        {
            if (this.domainServer.Nodes.PublicKeyChain.TryGetValue(publicKey, out var nodeProof))
            {
                return new(NetResult.Success, nodeProof.NetNode);
            }
            else
            {
                return new(NetResult.NotFound);
            }
        }
    }
}*/
