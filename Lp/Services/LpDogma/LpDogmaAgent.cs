// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;
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

    async NetTask<CredentialProof?> LpDogmaNetService.CreateCredentialProof(Value value, CredentialKind kind)
    {
        if (!this.IsAuthenticated)
        {
            return default;
        }

        CredentialState? state = kind switch
        {
            CredentialKind.Merger => this.merger.State,
            CredentialKind.Linker => this.linker.State,
            _ => default,
        };

        SeedKey? seedKey = kind switch
        {
            CredentialKind.Merger => this.merger.SeedKey,
            CredentialKind.Linker => this.linker.SeedKey,
            _ => default,
        };

        if (state is null || seedKey is null)
        {
            return default;
        }

        var proof = new CredentialProof(value, kind, state);
        if (seedKey.TrySign(proof, LpConstants.LpExpirationMics) &&
            proof.ValidateAndVerify())
        {
            return proof;
        }

        return default;
    }

    async NetTask<NetResult> LpDogmaNetService.AddCredentialEvidence(CredentialEvidence evidence)
    {
        if (!this.IsAuthenticated)
        {
            return NetResult.NotAuthenticated;
        }

        return this.credentials.Nodes.TryAdd(evidence) ? NetResult.Success : NetResult.InvalidData;
    }

    async NetTask<LinkageEvidence?> LpDogmaNetService.SignLinkageEvidence(LinkageEvidence evidence)
    {
        if (!this.IsAuthenticated)
        {
            return default;
        }

        if (this.merger.SeedKey.TrySign(evidence))
        {
            return evidence;
        }
        else
        {
            return default;
        }
    }

    async NetTask<Linkage?> LpDogmaNetService.SignLinkage(Linkage linkage)
    {
        if (!this.IsAuthenticated)
        {
            return default;
        }

        if (this.merger.SeedKey.TrySign(linkage, LpConstants.LpExpirationMics))
        {
            return linkage;
        }
        else
        {
            return default;
        }
    }
}
