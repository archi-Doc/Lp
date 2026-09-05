// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class FullCreditTest
{
    [Fact]
    public async Task MissingOwnerIsConsistentBetweenAsyncAndSynchronousLookups()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var credit = new Credit(default, [key]);
        var value = new Value(key, 1, credit);
        var proof = new EvolProof(key, value, value, null);
        var fullCredit = new FullCredit(credit);
        Assert.False(await fullCredit.ContainsAsync(proof));
        Assert.False(fullCredit.Contains(proof));
        fullCredit.Owners.Set(new OwnerData.GoshujinClass());
        Assert.False(await fullCredit.ContainsAsync(proof));
        Assert.False(fullCredit.Contains(proof));
    }
}
