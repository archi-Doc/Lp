// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IDomainRoot : INetService
{
    Task<NetResultAndValue<ConnectionAgreement?>> Authenticate(AuthenticationToken token);

    Task<MergedEvidence?> RequestMergedEvidence();

    Task<PeerProof?> ExchangeCertificateProof(PeerProof peerProof);
}

/*[NetServiceObject]
internal class MergerRemoteAgent : IDomainRoot
{
    private readonly LpBase lpBase;
    private readonly Merger merger;
    private bool authenticated;

    public MergerRemoteAgent(LpBase lpBase, Merger merger)
    {
        this.lpBase = lpBase;
        this.merger = merger;
    }

    async Task<NetResultAndValue<ConnectionAgreement?>> IMergerRemote.Authenticate(AuthenticationToken token)
    {
        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (!token.ValidateAndVerify(serverConnection) ||
            !token.PublicKey.Equals(this.lpBase.RemotePublicKey))
        {
            return new(NetResult.NotAuthenticated);
        }

        this.authenticated = true;

        return new((ConnectionAgreement?)default);
    }

    Task<T3csResult> IMergerRemote.CreateCredit(CreditIdentity creditIdentity)
    {
        if (!this.authenticated)
        {
            return Task.FromResult(T3csResult.NotAuthenticated);
        }

        return this.merger.CreateCredit(creditIdentity);
    }

    async Task<SignaturePublicKey> IMergerRemote.GetMergerKey()
    {
        if (!this.authenticated)
        {
            return default;
        }

        return this.merger.PublicKey;
    }
}*/
