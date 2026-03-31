// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

[TinyhandObject]
public readonly partial record struct DomainOverview(
    [property: Key(0)] int NumberOfPeers,
    [property: Key(1)] PeerProof? peerProof);

[NetService]
public interface IDomainService : INetService
{
    Task<NetResultAndValue<DomainOverview>> GetOverview(ulong domainHash);

    Task<(bool IsPeer, CertificateProof? NewProof)> Scout(ulong domainHash, CertificateToken<SignaturePublicKey>? token);

    Task<CertificateProof?> Exchange(ulong domainHash, CertificateProof? proof);

    void Radiate(ulong domainHash, CertificateProof proof, ref ResponseChannel<int> channel);
}
