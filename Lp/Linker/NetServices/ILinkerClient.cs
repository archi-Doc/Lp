// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[NetService]
public partial interface ILinkerClient : INetService
{
    Task<T3csResultAndValue<MergeableLinkage>> Evol(EvolProof evolProof);
}

[NetObject]
internal class LinkerClientAgent : ILinkerClient
{
    private readonly Merger merger;

    public LinkerClientAgent(Merger merger)
    {
        this.merger = merger;
    }

    Task<T3csResultAndValue<MergeableLinkage>> ILinkerClient.Evol(EvolProof evolProof)
    {
        throw new NotImplementedException();
    }
}
