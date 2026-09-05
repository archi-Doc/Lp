// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc;
using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public class ProofValidationTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void InvalidValidityIsRejectedBeforeSigning(int validity)
    {
        var key = SeedKey.NewSignature();
        var proof = new PeerProof(key.GetSignaturePublicKey());
        Assert.False(key.TrySign(proof, validity));
        Assert.Empty(proof.Signature);
        Assert.Equal(0, proof.SignedMics);
        var probe = new ProofProbe(validity);
        Assert.False(probe.Validate(ValidationOption.IgnoreSignatureBeforeSigning | ValidationOption.IgnoreExpiration));
    }

    [Fact]
    public void TamperedProofFailsVerification()
    {
        var key = SeedKey.NewSignature();
        var proof = new PeerProof(key.GetSignaturePublicKey());
        Assert.True(key.TrySign(proof, 60));
        Assert.True(proof.ValidateAndVerify());
        proof.Signature[0] ^= 1;
        Assert.False(proof.ValidateAndVerify());
    }

    [Fact]
    public void UnrelatedSignerWithTwoMergersIsRejectedWithoutThrowing()
    {
        var owner = SeedKey.NewSignature().GetSignaturePublicKey();
        var mergers = new[] { SeedKey.NewSignature().GetSignaturePublicKey(), SeedKey.NewSignature().GetSignaturePublicKey() };
        var value = new Value(owner, 1, new Credit(default, mergers));
        var publicKey = SeedKey.NewSignature().GetSignaturePublicKey();
        Assert.False(new SignerProbe(value, 0).PrepareForSigning(ref publicKey, 60));
        Assert.False(new ContractSignerProbe(value, 0).PrepareForSigning(ref publicKey, 60));
        publicKey = mergers[1];
        var proof = new SignerProbe(value, 0);
        Assert.True(proof.PrepareForSigning(ref publicKey, 60));
        Assert.True(proof.Validate(ValidationOption.IgnoreExpiration));
        Assert.Equal(publicKey, proof.GetSignatureKey());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(-2)]
    [InlineData(int.MaxValue)]
    public void SignerMustReferenceAnExistingMergerOrTheLpKey(int signer)
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var value = new Value(key, 1, new Credit(default, [key]));
        Assert.False(new SignerProbe(value, signer).Validate(ValidationOption.IgnoreSignatureBeforeSigning));
        Assert.False(new ContractSignerProbe(value, signer).Validate(ValidationOption.IgnoreSignatureBeforeSigning));
    }

    [Fact]
    public void MergedProofRejectsMissingMergerAndInvalidValue()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var value = new Value(key, 1, new Credit(default, [key]));
        Assert.False(new MergedProbe(value, 1).Validate(ValidationOption.IgnoreSignatureBeforeSigning));
        Assert.True(new MergedProbe(value, 0).Validate(ValidationOption.IgnoreSignatureBeforeSigning));
        Assert.False(new MergedProbe(new Value(key, 1, Credit.Default), 0).Validate(ValidationOption.IgnoreSignatureBeforeSigning));
    }

    [Fact]
    public void EvidenceRejectsNegativeMergerIndices()
    {
        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var proof = new TestLinkageProof(publicKey, new Value(publicKey, 1, new Credit(default, [publicKey])));
        Assert.True(key.TrySign(proof, 60));
        var evidence = new ContractableEvidence(true, proof, proof, Mics.GetCorrected(), Mics.GetCorrected() + Mics.FromSeconds(60));
        Assert.False(key.TrySign(evidence, -1));
        Assert.False(key.TrySign(evidence, 1));
        Assert.False(evidence.ValidateAndVerifyExceptProof(default, -1));
    }

    [Fact]
    public void LinkageVerifiesBothCreditsWhenReusingPooledEvidence()
    {
        var owner1 = SeedKey.NewSignature();
        var owner2 = SeedKey.NewSignature();
        var merger1 = SeedKey.NewSignature();
        var merger2 = SeedKey.NewSignature();
        var linker = SeedKey.NewSignature();
        var proof1 = new TestLinkageProof(linker.GetSignaturePublicKey(), new Value(owner1.GetSignaturePublicKey(), 1, new Credit(default, [merger1.GetSignaturePublicKey()])));
        var proof2 = new TestLinkageProof(linker.GetSignaturePublicKey(), new Value(owner2.GetSignaturePublicKey(), 2, new Credit(default, [merger2.GetSignaturePublicKey()])));
        Assert.True(owner1.TrySign(proof1, 60));
        Assert.True(owner2.TrySign(proof2, 60));
        var now = Mics.GetCorrected();
        var evidence1 = new ContractableEvidence(true, proof1, proof2, now, now + Mics.FromSeconds(60));
        var evidence2 = new ContractableEvidence(false, proof1, proof2, now, now + Mics.FromSeconds(60));
        Assert.True(merger1.TrySign(evidence1, 0));
        Assert.True(merger2.TrySign(evidence2, 0));
        Assert.True(Linkage.TryCreate(evidence1, evidence2, out var linkage));
        Assert.True(linker.TrySign(linkage, 60));
        for (var i = 0; i < 10; i++)
        {
            Assert.True(linkage.ValidateAndVerify());
        }

        var copy = TinyhandSerializer.Deserialize<Linkage>(TinyhandSerializer.Serialize(linkage));
        Assert.True(copy!.ValidateAndVerify());
        copy.MergerSignature10![0] ^= 1;
        Assert.False(copy.ValidateAndVerify());
    }

    private sealed class ProofProbe : Proof
    {
        public ProofProbe(int validity) => this.ValiditySeconds = validity;

        public override SignaturePublicKey GetSignatureKey() => default;
    }

    private sealed class SignerProbe : ProofWithSigner
    {
        public SignerProbe(Value value, int signer)
            : base(value)
        {
            this.Signer = signer;
            this.ValiditySeconds = 60;
        }

        public override PermittedSigner PermittedSigner => PermittedSigner.Merger | PermittedSigner.LpKey;
    }

    private sealed class ContractSignerProbe : ContractableProofWithSigner
    {
        public ContractSignerProbe(Value value, int signer)
            : base(default, value)
        {
            this.Signer = signer;
            this.ValiditySeconds = 60;
        }

        public override PermittedSigner PermittedSigner => PermittedSigner.Merger | PermittedSigner.LpKey;
    }

    private sealed class MergedProbe : MergedProof
    {
        public MergedProbe(Value value, byte index)
            : base(value)
        {
            this.MergerIndex = index;
            this.ValiditySeconds = 60;
        }
    }
}
