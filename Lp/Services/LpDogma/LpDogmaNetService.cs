﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

[NetServiceInterface]
public partial interface LpDogmaNetService : INetService
{
    NetTask<NetResultAndValue<ConnectionAgreement?>> Authenticate(AuthenticationToken token);

    NetTask<LpDogmaInformation?> GetInformation();

    // NetTask<CredentialProof?> CreateCredentialProof(Value value, CredentialKind credentialKind);

    NetTask<NetResult> AddCredentialEvidence(CredentialEvidence evidence);

    NetTask<ContractableEvidence?> SignContractableEvidence(ContractableEvidence evidence);

    NetTask<LinkLinkage?> SignLinkage(LinkLinkage linkage);

    // NetTask<Linkage?> CreateLink(LinkProof linkProof1, LinkProof linkProof2);
}
