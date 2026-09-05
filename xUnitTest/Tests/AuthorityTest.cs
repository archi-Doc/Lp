// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class AuthorityTest
{
    [Fact]
    public void CachedSeedKeysRemainStableAcrossEqualCredits()
    {
        var authority = new Authority(new byte[32], AuthorityLifecycle.Application, 0);
        var merger = SeedKey.NewSignature().GetSignaturePublicKey();
        var credit1 = new Credit(default, [merger]);
        var credit2 = new Credit(default, [merger]);
        Assert.Same(authority.GetSeedKey(), authority.GetSeedKey());
        Assert.Same(authority.GetSeedKey(credit1), authority.GetSeedKey(credit2));
        Assert.NotEqual(authority.GetSignaturePublicKey(), authority.GetSignaturePublicKey(credit1));
        Assert.False(authority.IsExpired);
    }
}
