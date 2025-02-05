// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Stats;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IMergerRemote : INetService
{
    NetTask<(NetResult Result, ConnectionAgreement? Agreement)> Authenticate(AuthenticationToken token);

    NetTask<Proof?> NewCredential(Evidence? evidence);

    NetTask<SignaturePublicKey> GetMergerKey();
}

[NetServiceObject]
internal class MergerRemoteAgent : IMergerRemote
{
    private readonly LpBase lpBase;
    private readonly Merger merger;
    private readonly SignaturePublicKey remotePublicKey;
    private readonly NetStats netStats;
    private bool authenticated;

    private bool IsActiveAndAuthenticated => this.merger.State.IsActive && this.authenticated;

    public MergerRemoteAgent(LpBase lpBase, Merger merger, NetStats netStats)
    {
        this.lpBase = lpBase;
        this.merger = merger;
        this.lpBase.TryGetRemotePublicKey(out this.remotePublicKey);
        this.netStats = netStats;
    }

    async NetTask<(NetResult Result, ConnectionAgreement? Agreement)> IMergerRemote.Authenticate(AuthenticationToken token)
    {
        if (!this.merger.State.IsActive)
        {
            return (NetResult.NoNetService, default);
        }

        if (!this.lpBase.TryGetRemotePublicKey(out var publicKey))
        {
            return (NetResult.NotAuthenticated, default);
        }

        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (token.PublicKey.Equals(publicKey) &&
            token.ValidateAndVerifyWithSalt(serverConnection.EmbryoSalt))
        {
            // Console.WriteLine("Authentication success");
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

    async NetTask<SignaturePublicKey> IMergerRemote.GetMergerKey()
    {
        Console.WriteLine(this.IsActiveAndAuthenticated);
        Console.WriteLine(this.merger.GetMergerKey().ToString());
        if (!this.IsActiveAndAuthenticated)
        {
            return default;
        }

        return this.merger.GetMergerKey();
    }

    async NetTask<Proof?> IMergerRemote.NewCredential(Evidence? evidence)
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

        /*if (!TransmissionContext.Current.AuthenticationTokenEquals(this.remotePublicKey))
        {
            return T3csResult.NotAuthenticated;
        }*/

        return default;
    }
}
