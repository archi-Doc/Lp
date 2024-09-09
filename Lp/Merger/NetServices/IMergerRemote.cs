﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

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
    private bool authenticated;

    public MergerRemoteAgent(LpBase lpBase, Merger merger)
    {
        this.lpBase = lpBase;
        this.merger = merger;
        this.lpBase.TryGetRemotePublicKey(out this.remotePublicKey);
    }

    async NetTask<NetResult> IMergerRemote.Authenticate(AuthenticationToken token)
    {
        if (!this.lpBase.TryGetRemotePublicKey(out var publicKey))
        {
            return NetResult.NotAuthenticated;
        }

        if (token.ValidateAndVerifyWithSalt(TransmissionContext.Current.ServerConnection.Salt))
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
        {// Create Lp ValueProof
            if (!Value.TryCreate(this.merger.GetPublicKey(), 1, LpConstants.LpCredit, out var value))
            {
                return default;
            }

            var valueProof = ValueProof.Create(value);
            this.merger.SignProof(valueProof, Mics.FromDays(1));
            // valueProof.SignProof()
            return valueProof;
        }
        else
        {// Create CredentialProof
            var credentialProof = CredentialProof.Create(evidence, this.merger.);

            return default;
        }

        /*if (!TransmissionContext.Current.AuthenticationTokenEquals(this.remotePublicKey))
        {
            return T3csResult.NotAuthenticated;
        }*/

        return default;
    }
}