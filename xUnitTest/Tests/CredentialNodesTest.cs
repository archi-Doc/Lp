// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class CredentialNodesTest
{
    [Fact]
    public void AnEmptyCreditIsNotAuthorized()
    {
        var nodes = new CredentialNodes();
        Assert.Equal(0, Credit.Default.MergerCount);
        Assert.False(nodes.CheckAuthorization(Credit.Default));
    }

    [Fact]
    public void AnUnknownMergerIsNotAuthorized()
    {
        var nodes = new CredentialNodes();
        var merger = SeedKey.NewSignature().GetSignaturePublicKey();
        Assert.False(nodes.CheckAuthorization(merger));
        Assert.False(nodes.CheckAuthorization(new Credit(default, [merger])));
        Assert.False(nodes.TryGet(merger, out _));
    }

    [Fact]
    public void ADefaultPublicKeyIsNotAuthorized()
    {
        var nodes = new CredentialNodes();
        Assert.False(nodes.CheckAuthorization(default(SignaturePublicKey)));
    }

    [Fact]
    public void UnsignedEvidenceIsRejected()
    {
        var nodes = new CredentialNodes();
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var proof = new CredentialProof(key, CredentialKind.Merger, new MergerState());
        Assert.False(nodes.TryAdd(new CredentialEvidence(proof)));
        Assert.Empty(nodes.ToArray());
    }

    [Fact]
    public void ValidateLeavesAnEmptyCollectionUntouched()
    {
        var nodes = new CredentialNodes();
        nodes.Validate();
        Assert.Empty(nodes.ToArray());
    }

    [Fact]
    public void LinksStartEmptyAndReportUnknownLinkers()
    {
        var links = new CredentialLinks();
        links.Validate();
        Assert.Empty(links.ToArray());
        Assert.False(links.TryGet(SeedKey.NewSignature().GetSignaturePublicKey(), out _));
    }
}
