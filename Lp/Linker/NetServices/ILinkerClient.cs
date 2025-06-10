// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface ILinkerClient : INetService
{
    NetTask<T3csResultAndValue<MergeableLinkage>> Evol(EvolProof evolProof);
}

[NetServiceObject]
internal class LinkerClientAgent : ILinkerClient
{
    private readonly Merger merger;

    public LinkerClientAgent(Merger merger)
    {
        this.merger = merger;
    }

    NetTask<T3csResultAndValue<MergeableLinkage>> ILinkerClient.Evol(EvolProof evolProof)
    {
        throw new NotImplementedException();
    }
}
