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

    Task<T3csResult> AssignDomain(DomainAssignment domainAssignment);
}

[NetServiceObject]
public class MergerAdministrationAgent : IMergerAdministration
{
    public LpBase LpBase { get; }

    public Merger Merger { get; }

    public bool IsAuthenticated { get; private set; }

    public MergerAdministrationAgent(LpBase lpBase, Merger merger)
    {
        this.LpBase = lpBase;
        this.Merger = merger;
    }

    async Task<NetResultAndValue<ConnectionAgreement?>> IMergerAdministration.Authenticate(AuthenticationToken token)
    {
        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (!token.ValidateAndVerify(serverConnection) ||
            !token.PublicKey.Equals(this.LpBase.RemotePublicKey))
        {
            return new(NetResult.NotAuthenticated);
        }

        this.IsAuthenticated = true;

        return new((ConnectionAgreement?)default);
    }

    Task<T3csResult> IMergerAdministration.CreateCredit(CreditIdentity creditIdentity)
    {
        if (!this.IsAuthenticated)
        {
            return Task.FromResult(T3csResult.NotAuthenticated);
        }

        return this.Merger.CreateCredit(creditIdentity);
    }

    async Task<SignaturePublicKey> IMergerAdministration.GetMergerKey()
    {
        if (!this.IsAuthenticated)
        {
            return default;
        }

        return this.Merger.PublicKey;
    }

    Task<T3csResult> IMergerAdministration.AssignDomain(DomainAssignment domainAssignment)
    {
        if (!this.IsAuthenticated)
        {
            return Task.FromResult(T3csResult.NotAuthenticated);
        }

        return Task.FromResult(T3csResult.Success);
    }
}
