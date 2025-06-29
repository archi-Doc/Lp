// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class CreditNetTest
{
    [Fact]
    public void TestMergerProof()
    {
        var seedKey = SeedKey.NewSignature();
        var publicKey = seedKey.GetSignaturePublicKey();
        var mergerProof = new MergerProof(publicKey);
        var validMics = Mics.FromMinutes(1);

        seedKey.TrySign(mergerProof, validMics).IsTrue();
        mergerProof.ValidateAndVerify().IsTrue();

        mergerProof = new MergerProof(publicKey, true); // LpKey
        seedKey.TrySign(mergerProof, validMics).IsFalse();
    }
}
