// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class SeedKeySigningTest
{
    [Fact]
    public void SigningAProofWithAPublicKeyRebindsItToTheSigner()
    {
        var owner = SeedKey.NewSignature();
        var stranger = SeedKey.NewSignature();
        var proof = new PeerProof(owner.GetSignaturePublicKey());

        // ProofWithPublicKey identifies its signer, so any key may sign and takes ownership.
        Assert.True(stranger.TrySign(proof, 60));
        Assert.Equal(stranger.GetSignaturePublicKey(), proof.GetSignatureKey());
        Assert.True(proof.ValidateAndVerify());

        Assert.True(owner.TrySign(proof, 60));
        Assert.Equal(owner.GetSignaturePublicKey(), proof.GetSignatureKey());
        Assert.True(owner.TrySignAndValidate(new PeerProof(owner.GetSignaturePublicKey()), 60));
    }

    [Fact]
    public void SigningAProofBoundToAValueRequiresAPermittedSigner()
    {
        var owner = SeedKey.NewSignature();
        var stranger = SeedKey.NewSignature();
        var ownerKey = owner.GetSignaturePublicKey();
        var value = new Value(ownerKey, 1, new Credit(default, [ownerKey]));

        Assert.False(stranger.TrySign(new TestLinkageProof(default, value), 60));
        Assert.True(owner.TrySign(new TestLinkageProof(default, value), 60));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void SigningRejectsAValidityOutsideTheAllowedRange(int validitySeconds)
    {
        var key = SeedKey.NewSignature();
        var proof = new PeerProof(key.GetSignaturePublicKey());
        Assert.False(key.TrySign(proof, validitySeconds));
        Assert.False(key.TrySignAndValidate(proof, validitySeconds));
    }

    [Fact]
    public void SigningEvidenceRequiresAMergerOfTheCredit()
    {
        var owner = SeedKey.NewSignature();
        var merger = SeedKey.NewSignature();
        var stranger = SeedKey.NewSignature();
        var evidence = CreateEvidence(owner, merger);

        Assert.False(stranger.TrySign(evidence));
        Assert.False(stranger.TrySign(evidence, 0));
        Assert.True(merger.TrySign(evidence, 0));
        Assert.NotNull(evidence.GetSignature(0));
    }

    [Fact]
    public void SigningEvidenceIsIdempotent()
    {
        var owner = SeedKey.NewSignature();
        var merger = SeedKey.NewSignature();
        var evidence = CreateEvidence(owner, merger);

        Assert.True(merger.TrySign(evidence));
        var signature = evidence.GetSignature(0);
        Assert.NotNull(signature);

        // A second attempt keeps the existing signature instead of producing a new one.
        Assert.True(merger.TrySign(evidence));
        Assert.Same(signature, evidence.GetSignature(0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void SigningEvidenceRejectsAnOutOfRangeMergerIndex(int mergerIndex)
    {
        var owner = SeedKey.NewSignature();
        var merger = SeedKey.NewSignature();
        Assert.False(merger.TrySign(CreateEvidence(owner, merger), mergerIndex));
    }

    [Fact]
    public void SigningALinkageRequiresTheLinkerKey()
    {
        var owner = SeedKey.NewSignature();
        var merger = SeedKey.NewSignature();
        var linker = SeedKey.NewSignature();
        var ownerKey = owner.GetSignaturePublicKey();
        var credit = new Credit(default, [merger.GetSignaturePublicKey()]);

        var proof1 = new TestLinkageProof(linker.GetSignaturePublicKey(), new Value(ownerKey, 1, credit));
        var proof2 = new TestLinkageProof(linker.GetSignaturePublicKey(), new Value(ownerKey, 2, credit));
        Assert.True(owner.TrySign(proof1, 60));
        Assert.True(owner.TrySign(proof2, 60));

        var now = Mics.GetCorrected();
        var expiration = now + Mics.FromSeconds(60);
        var evidence1 = new ContractableEvidence(true, proof1, proof2, now, expiration);
        var evidence2 = new ContractableEvidence(false, proof1, proof2, now, expiration);
        Assert.True(merger.TrySign(evidence1));
        Assert.True(merger.TrySign(evidence2));
        Assert.True(Linkage.TryCreate(evidence1, evidence2, out var linkage));

        Assert.False(owner.TrySign(linkage));
        Assert.True(linker.TrySign(linkage));
        Assert.True(linkage.ValidateAndVerify());
    }

    [Fact]
    public void LinkageCreationRequiresExactlyOnePrimaryEvidence()
    {
        var owner = SeedKey.NewSignature();
        var merger = SeedKey.NewSignature();
        var linker = SeedKey.NewSignature().GetSignaturePublicKey();
        var ownerKey = owner.GetSignaturePublicKey();
        var credit = new Credit(default, [merger.GetSignaturePublicKey()]);

        var proof = new TestLinkageProof(linker, new Value(ownerKey, 1, credit));
        Assert.True(owner.TrySign(proof, 60));
        var now = Mics.GetCorrected();
        var expiration = now + Mics.FromSeconds(60);

        var primary = new ContractableEvidence(true, proof, proof, now, expiration);
        var secondary = new ContractableEvidence(false, proof, proof, now, expiration);
        Assert.True(merger.TrySign(primary));
        Assert.True(merger.TrySign(secondary));

        Assert.False(Linkage.TryCreate(primary, primary, out _));
        Assert.False(Linkage.TryCreate(secondary, secondary, out _));

        // The order of the arguments does not matter.
        Assert.True(Linkage.TryCreate(secondary, primary, out var linkage));
        Assert.True(Linkage.TryCreate(primary, secondary, out var linkage2));
        Assert.Equal(linkage.LinkedMics, linkage2.LinkedMics);
    }

    [Fact]
    public void LinkageCreationRejectsMismatchedTimestamps()
    {
        var owner = SeedKey.NewSignature();
        var merger = SeedKey.NewSignature();
        var linker = SeedKey.NewSignature().GetSignaturePublicKey();
        var ownerKey = owner.GetSignaturePublicKey();
        var credit = new Credit(default, [merger.GetSignaturePublicKey()]);

        var proof = new TestLinkageProof(linker, new Value(ownerKey, 1, credit));
        Assert.True(owner.TrySign(proof, 60));
        var now = Mics.GetCorrected();
        var expiration = now + Mics.FromSeconds(60);

        var primary = new ContractableEvidence(true, proof, proof, now, expiration);
        var secondary = new ContractableEvidence(false, proof, proof, now + 1, expiration);
        Assert.True(merger.TrySign(primary));
        Assert.True(merger.TrySign(secondary));
        Assert.False(Linkage.TryCreate(primary, secondary, out _));

        var differentExpiration = new ContractableEvidence(false, proof, proof, now, expiration + 1);
        Assert.True(merger.TrySign(differentExpiration));
        Assert.False(Linkage.TryCreate(primary, differentExpiration, out _));
    }

    [Fact]
    public void EvidenceReportsTheMergerIndexOfAKnownMerger()
    {
        var owner = SeedKey.NewSignature();
        var merger = SeedKey.NewSignature();
        var evidence = CreateEvidence(owner, merger);

        var mergerKey = merger.GetSignaturePublicKey();
        var (proof, index) = evidence.GetMergerIndex(ref mergerKey);
        Assert.NotNull(proof);
        Assert.Equal(0, index);

        var strangerKey = SeedKey.NewSignature().GetSignaturePublicKey();
        (proof, index) = evidence.GetMergerIndex(ref strangerKey);
        Assert.Null(proof);
        Assert.Equal(0, index);
    }

    private static ContractableEvidence CreateEvidence(SeedKey owner, SeedKey merger)
    {
        var ownerKey = owner.GetSignaturePublicKey();
        var credit = new Credit(default, [merger.GetSignaturePublicKey()]);
        var proof = new TestLinkageProof(default, new Value(ownerKey, 1, credit));
        Assert.True(owner.TrySign(proof, 60));
        var now = Mics.GetCorrected();
        return new ContractableEvidence(true, proof, proof, now, now + Mics.FromSeconds(60));
    }
}
