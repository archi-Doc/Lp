// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IMergerAdministration : INetService
{
    Task<NetResultAndValue<ConnectionAgreement?>> Authenticate(AuthenticationToken token);

    Task<T3csResult> CreateCredit(CreditIdentity creditIdentity);

    Task<SignaturePublicKey> GetMergerKey();
}

[NetServiceObject]
internal class MergerRemoteAgent : IMergerAdministration
{
    private readonly LpBase lpBase;
    private readonly Merger merger;
    private bool authenticated;

    public MergerRemoteAgent(LpBase lpBase, Merger merger)
    {
        this.lpBase = lpBase;
        this.merger = merger;
    }

    async Task<NetResultAndValue<ConnectionAgreement?>> IMergerAdministration.Authenticate(AuthenticationToken token)
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

    Task<T3csResult> IMergerAdministration.CreateCredit(CreditIdentity creditIdentity)
    {
        if (!this.authenticated)
        {
            return Task.FromResult(T3csResult.NotAuthenticated);
        }

        return this.merger.CreateCredit(creditIdentity);
    }

    async Task<SignaturePublicKey> IMergerAdministration.GetMergerKey()
    {
        if (!this.authenticated)
        {
            return default;
        }

        return this.merger.PublicKey;
    }
}
