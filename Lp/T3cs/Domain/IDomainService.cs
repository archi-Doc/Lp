// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;

namespace Lp.Net;

[TinyhandObject]
public readonly partial record struct DomainOverview(
    [property: Key(0)] int NumberOfPeers,
    [property: Key(1)] PeerProof? peerProof);

[NetServiceInterface]
public interface IDomainService : INetService
{
    Task<NetResultAndValue<DomainOverview>> GetOverview(ulong domainHash);
}
