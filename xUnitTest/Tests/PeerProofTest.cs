// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class PeerProofTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    [InlineData(101)]
    public void RandomSelectionStaysInsideTheList(int count)
    {
        var peers = new PeerProof.GoshujinClass();
        var expected = new HashSet<PeerProof>();
        for (var i = 0; i < count; i++)
        {
            var peer = new PeerProof(SeedKey.NewSignature().GetSignaturePublicKey());
            peers.Add(peer);
            expected.Add(peer);
        }

        for (var i = 0; i < 1000; i++)
        {
            var peer = peers.GetRandomInternal();
            if (count == 0)
            {
                Assert.Null(peer);
            }
            else
            {
                Assert.NotNull(peer);
                Assert.Contains(peer, expected);
            }
        }
    }
}
