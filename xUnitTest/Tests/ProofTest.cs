// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class ProofTest
{
    [Fact]
    public void Test1()
    {
        var ownerKey = SeedKey.NewSignature();
        var owner = ownerKey.GetSignaturePublicKey();
        var mergerKey = SeedKey.NewSignature();
        var merger = mergerKey.GetSignaturePublicKey();

        var creditIdentity = new Identity(IdentityKind.Credit, owner, [merger,]);
        Credit.TryCreate(creditIdentity, out var credit).IsTrue();
        var value = new Value(owner, 2, credit!);
        var state = new MergerState();

        var credentialProof = new CredentialProof(value, CredentialKind.Merger, state);
        ownerKey.TrySign(credentialProof, 3).IsTrue();
        credentialProof.ValidateAndVerify().IsTrue();
    }
}
