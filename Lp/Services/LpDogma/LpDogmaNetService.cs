// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

[NetServiceInterface]
public partial interface LpDogmaNetService : INetService
{
    NetTask<(NetResult Result, ConnectionAgreement? Agreement)> Authenticate(AuthenticationToken token);

    NetTask<LpDogmaInformation?> GetInformation();

    NetTask<CredentialProof?> NewCredentialProof(CertificateToken<Value> token);
}
