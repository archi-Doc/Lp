// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Stats;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IMergerRemote : INetService
{
    NetTask<NetResult> Authenticate(AuthenticationToken token);

    NetTask<Proof?> NewCredential(Evidence? evidence);

    NetTask<SignaturePublicKey> GetPublicKey();
}

[NetServiceObject]
internal class MergerRemoteAgent : IMergerRemote
{
    private readonly LpBase lpBase;
    private readonly Merger merger;
    private readonly SignaturePublicKey remotePublicKey;
    private readonly NetStats netStats;
    private bool authenticated;

    public MergerRemoteAgent(LpBase lpBase, Merger merger, NetStats netStats)
    {
        this.lpBase = lpBase;
        this.merger = merger;
        this.lpBase.TryGetRemotePublicKey(out this.remotePublicKey);
        this.netStats = netStats;
    }

    async NetTask<NetResult> IMergerRemote.Authenticate(AuthenticationToken token)
    {
        if (!this.lpBase.TryGetRemotePublicKey(out var publicKey))
        {
            return NetResult.NotAuthenticated;
        }

        if (token.PublicKey.Equals(publicKey) &&
            token.ValidateAndVerifyWithSalt(TransmissionContext.Current.ServerConnection.EmbryoSalt))
        {
            // Console.WriteLine("Authentication success");
            this.authenticated = true;
            return NetResult.Success;
        }
        else
        {
            this.authenticated = false;
            return NetResult.NotAuthenticated;
        }
    }

    async NetTask<SignaturePublicKey> IMergerRemote.GetPublicKey()
    {
        if (!this.authenticated)
        {
            return default;
        }

        return this.merger.GetPublicKey();
    }

    async NetTask<Proof?> IMergerRemote.NewCredential(Evidence? evidence)
    {
        if (!this.authenticated)
        {
            return default;
        }

        if (evidence == null)
        {// Create ValueProof
            if (!Value.TryCreate(this.merger.GetPublicKey(), 1, LpConstants.LpCredit, out var value))
            {
                return default;
            }

            var valueProof = ValueProof.Create(value);
            this.merger.TrySignProof(valueProof, CredentialProof.LpExpirationMics);
            return valueProof;
        }
        else if (evidence.Validate())
        {// Evidence(ValueProof) -> CredentialProof
            var netNode = this.netStats.GetOwnNetNode();
            var credentialProof = CredentialProof.Create(evidence, netNode);
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
