// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

[NetServiceInterface]
public partial interface LpDogmaNetService : INetService
{
    Task<NetResultAndValue<ConnectionAgreement?>> Authenticate(AuthenticationToken token);

    Task<LpDogmaInformation?> GetInformation();

    // Task<CredentialProof?> CreateCredentialProof(Value value, CredentialKind credentialKind);

    Task<NetResult> AddCredentialEvidence(CredentialEvidence evidence);

    Task<ContractableEvidence?> SignContractableEvidence(ContractableEvidence evidence);

    Task<LinkLinkage?> SignLinkage(LinkLinkage linkage);

    // Task<Linkage?> CreateLink(LinkProof linkProof1, LinkProof linkProof2);
}
