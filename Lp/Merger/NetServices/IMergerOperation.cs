// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IMergerOperation : INetService
{
    NetTask<T3csResult> SendValueProofEvidence(Evidence evidence);
}

[NetServiceObject]
internal class MergerOperationAgent : IMergerOperation
{
    public MergerOperationAgent(Merger merger)
    {
        this.merger = merger;
    }

    private Merger merger;

    async NetTask<T3csResult> IMergerOperation.SendValueProofEvidence(Evidence evidence)
    {
        return T3csResult.Success;
    }
}
