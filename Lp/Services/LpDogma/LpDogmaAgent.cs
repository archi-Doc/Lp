// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

[NetServiceObject]
internal class LpDogmaAgent : LpDogmaNetService
{
    private readonly NetBase netBase;
    private readonly LpBase lpBase;
    private readonly Merger merger;
    private readonly RelayMerger relayMerger;
    private readonly Linker linker;
    private readonly Credentials credentials;
    private bool authenticated;

    private bool IsActiveAndAuthenticated => this.merger.State.IsActive && this.authenticated;

    private bool IsAuthenticated => this.authenticated;

    public LpDogmaAgent(NetBase netBase, LpBase lpBase, Merger merger, RelayMerger relayMerger, Credentials credentials, Linker linker)
    {
        this.netBase = netBase;
        this.lpBase = lpBase;
        this.merger = merger;
        this.relayMerger = relayMerger;
        this.linker = linker;
        this.credentials = credentials;
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

    async NetTask<LpDogmaInformation?> LpDogmaNetService.GetInformation()
    {
        if (!this.IsAuthenticated)
        {
            return default;
        }

        var info = new LpDogmaInformation(this.netBase.NodePublicKey, this.merger.PublicKey, this.relayMerger.PublicKey, this.linker.PublicKey);
        return info;
    }

    async NetTask<CredentialProof?> LpDogmaNetService.CreateMergerCredentialProof(CertificateToken<Value> token)
    {
        if (!this.IsAuthenticated)
        {
            return default;
        }

        if (!token.ValidateAndVerify(TransmissionContext.Current.ServerConnection) ||
            !token.PublicKey.Equals(LpConstants.LpPublicKey))
        {
            return default;
        }

        var credentialProof = new CredentialProof(token.Target, this.merger.State);
        if (!this.merger.TrySign(credentialProof, CredentialProof.LpExpirationMics) ||
            !credentialProof.ValidateAndVerify())
        {
            return default;
        }

        return credentialProof;
    }

    async NetTask<CredentialProof?> LpDogmaNetService.CreateLinkerCredentialProof(CertificateToken<Value> token)
    {
        if (!this.IsAuthenticated)
        {
            return default;
        }

        if (!token.ValidateAndVerify(TransmissionContext.Current.ServerConnection) ||
            !token.PublicKey.Equals(LpConstants.LpPublicKey))
        {
            return default;
        }

        var credentialProof = new CredentialProof(token.Target, this.merger.State);
        if (!this.merger.TrySign(credentialProof, CredentialProof.LpExpirationMics) ||
            !credentialProof.ValidateAndVerify())
        {
            return default;
        }

        return credentialProof;
    }

    async NetTask<NetResult> LpDogmaNetService.AddMergerCredentialEvidence(CredentialEvidence evidence)
    {
        if (!this.IsAuthenticated)
        {
            return NetResult.NotAuthenticated;
        }

        if (this.credentials.MergerCredentials.TryAdd(evidence))
        {
            return NetResult.Success;
        }
        else
        {
            return NetResult.InvalidData;
        }
    }

    async NetTask<NetResult> LpDogmaNetService.AddLinkerCredentialEvidence(CredentialEvidence evidence)
    {
        if (!this.IsAuthenticated)
        {
            return NetResult.NotAuthenticated;
        }

        if (this.credentials.LinkerCredentials.TryAdd(evidence))
        {
            return NetResult.Success;
        }
        else
        {
            return NetResult.InvalidData;
        }
    }
}
