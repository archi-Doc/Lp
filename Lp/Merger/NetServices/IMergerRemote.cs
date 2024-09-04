// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IMergerRemote : INetService
{
    NetTask<T3csResult> SendValueProofEvidence(int evidence);
}

[NetServiceObject]
internal class MergerRemoteAgent : IMergerRemote
{
    private readonly LpBase lpBase;
    private readonly Merger merger;
    private readonly SignaturePublicKey remotePublicKey;

    public MergerRemoteAgent(LpBase lpBase, Merger merger)
    {
        this.lpBase = lpBase;
        this.merger = merger;
        this.lpBase.TryGetRemotePublicKey(out this.remotePublicKey);
    }

    async NetTask<T3csResult> IMergerRemote.SendValueProofEvidence(/*Evidence? evidence*/ int x)
    {
        if (!TransmissionContext.Current.AuthenticationTokenEquals(this.remotePublicKey))
        {
            return T3csResult.NotAuthenticated;
        }

        return T3csResult.Success;
    }
}
