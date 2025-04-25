// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

[NetServiceObject]
internal class LpDogmaAgent : LpDogmaNetService
{
    private readonly LpBase lpBase;
    private readonly Merger merger;
    private bool authenticated;

    private bool IsActiveAndAuthenticated => this.merger.State.IsActive && this.authenticated;

    public LpDogmaAgent(LpBase lpBase, Merger merger)
    {
        this.lpBase = lpBase;
        this.merger = merger;
    }

    async NetTask<(NetResult Result, ConnectionAgreement? Agreement)> LpDogmaNetService.Authenticate(AuthenticationToken token)
    {
        /*if (!this.merger.State.IsActive)
        {
            return (NetResult.NoNetService, default);
        }*/

        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (token.PublicKey.Equals(LpConstants.LpPublicKey) &&
            token.ValidateAndVerify(serverConnection))
        {
            serverConnection.Agreement.MinimumConnectionRetentionMics = Mics.FromMinutes(10);
            this.authenticated = true;
            return (NetResult.Success, serverConnection.Agreement);
        }
        else
        {
            this.authenticated = false;
            return (NetResult.NotAuthenticated, default);
        }
    }

    async NetTask<SignaturePublicKey> LpDogmaNetService.GetMergerKey()
    {
        if (!this.IsActiveAndAuthenticated)
        {
            return default;
        }

        return this.merger.GetMergerKey();
    }

    /*async NetTask<Proof?> IMergerRemote.NewCredential(Evidence? evidence)
    {
        if (!this.IsActiveAndAuthenticated)
        {
            return default;
        }

        if (evidence == null)
        {// Create ValueProof
            if (!Value.TryCreate(this.merger.GetMergerKey(), 1, LpConstants.LpCredit, out var value))
            {
                return default;
            }

            var valueProof = ValueProof.Create(value);
            this.merger.TrySignProof(valueProof, CredentialProof.LpExpirationMics);
            return valueProof;
        }
        else if (evidence.Validate())
        {// Evidence(ValueProof) -> CredentialProof
            var credentialProof = CredentialProof.New(default, this.merger.State);
            this.merger.TrySignProof(credentialProof, CredentialProof.LpExpirationMics);
            return credentialProof;
        }

        return default;
    }*/

    async NetTask<CredentialProof?> LpDogmaNetService.NewCredentialProof(CertificateToken<Value> token)
    {
        if (!this.IsActiveAndAuthenticated)
        {
            return default;
        }

        if (!token.ValidateAndVerify(TransmissionContext.Current.ServerConnection) ||
            !token.PublicKey.Equals(LpConstants.LpPublicKey))
        {
            return default;
        }

        var credentialProof = new CredentialProof(this.merger.MergerPublicKey, token.Target, this.merger.State);
        if (!this.merger.TrySign(credentialProof, CredentialProof.LpExpirationMics) ||
            !credentialProof.ValidateAndVerify())
        {
            return default;
        }

        return credentialProof;
    }
}
