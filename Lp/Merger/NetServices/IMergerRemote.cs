// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IMergerRemote : INetService
{
    Task<NetResultAndValue<ConnectionAgreement?>> Authenticate(AuthenticationToken token);

    Task<NetResult> CreateCredit(CreditIdentity creditIdentity);
}

[NetServiceObject]
internal class MergerRemoteAgent : IMergerRemote
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

    async Task<NetResult> IMergerRemote.CreateCredit(CreditIdentity creditIdentity)
    {
        if (!this.authenticated)
        {
            return NetResult.NotAuthenticated;
        }

        var fullCredit = await this.merger.GetOrCreateCredit(creditIdentity);
        return fullCredit is not null ? NetResult.Success : NetResult.InvalidData;
    }
}
