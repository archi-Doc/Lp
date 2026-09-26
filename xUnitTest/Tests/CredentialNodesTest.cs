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
    public void RemovingAnEvidenceKeepsAnAliasItDidNotAdd()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var existingAlias = "A" + Guid.NewGuid().ToString("N").Substring(0, 12);
        Alias.Instance.Add(existingAlias, key);
        try
        {
            var goshujin = new CredentialEvidence.GoshujinClass() { SyncAlias = true, };
            var evidence = new CredentialEvidence(new CredentialProof(key, CredentialKind.Merger, new MergerState() { Name = "B" + Guid.NewGuid().ToString("N").Substring(0, 12), }));
            using (goshujin.LockObject.EnterScope())
            {
                evidence.Goshujin = goshujin; // The key already has an alias, so none is added.
                evidence.Goshujin = null;
            }

            Assert.True(Alias.Instance.TryGetAliasFromPublicKey(key, out var alias));
            Assert.Equal(existingAlias, alias);
        }
        finally
        {
            Alias.Instance.Remove(key);
        }
    }

    [Fact]
    public void AnAliasAddedByAnEvidenceIsRemovedWithIt()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var name = "C" + Guid.NewGuid().ToString("N").Substring(0, 12);
        var goshujin = new CredentialEvidence.GoshujinClass() { SyncAlias = true, };
        var evidence = new CredentialEvidence(new CredentialProof(key, CredentialKind.Merger, new MergerState() { Name = name, }));
        using (goshujin.LockObject.EnterScope())
        {
            evidence.Goshujin = goshujin;
            Assert.True(Alias.Instance.TryGetAliasFromPublicKey(key, out var alias));
            Assert.Equal(name, alias);

            evidence.Goshujin = null;
        }

        Assert.False(Alias.Instance.TryGetAliasFromPublicKey(key, out _));
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
