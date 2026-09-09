// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public class ProofEqualityTest
{
    [Fact]
    public void ProofEqualityComparesSignatureContentNotReference()
    {
        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var proof = new PeerProof(publicKey);
        Assert.True(key.TrySign(proof, 60));

        var copy = TinyhandSerializer.Deserialize<PeerProof>(TinyhandSerializer.Serialize(proof))!;
        Assert.NotSame(proof.Signature, copy.Signature);
        Assert.True(proof.Equals(copy));
        Assert.False(proof.Equals(null));

        copy.Signature[0] ^= 1;
        Assert.False(proof.Equals(copy));
    }

    [Fact]
    public void ProofsWithSignaturesOfDifferentLengthAreNotEqual()
    {
        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var signed = new PeerProof(publicKey);
        Assert.True(key.TrySign(signed, 60));

        var unsigned = new PeerProof(publicKey);
        Assert.Empty(unsigned.Signature);
        Assert.False(signed.Equals(unsigned));
        Assert.False(unsigned.Equals(signed));
        Assert.True(unsigned.Equals(new PeerProof(publicKey)));
    }

    [Fact]
    public void ProofsSignedByDifferentKeysAreNotEqual()
    {
        var key1 = SeedKey.NewSignature();
        var key2 = SeedKey.NewSignature();
        var proof1 = new PeerProof(key1.GetSignaturePublicKey());
        var proof2 = new PeerProof(key2.GetSignaturePublicKey());
        Assert.True(key1.TrySign(proof1, 60));
        Assert.True(key2.TrySign(proof2, 60));
        Assert.False(proof1.Equals(proof2));
    }

    [Fact]
    public void ContractableProofEqualityIncludesTheLinkerKey()
    {
        var owner = SeedKey.NewSignature();
        var ownerKey = owner.GetSignaturePublicKey();
        var linker1 = SeedKey.NewSignature().GetSignaturePublicKey();
        var linker2 = SeedKey.NewSignature().GetSignaturePublicKey();
        var value = new Value(ownerKey, 1, new Credit(default, [ownerKey]));

        var proof1 = new TestLinkageProof(linker1, value);
        var proof2 = new TestLinkageProof(linker2, value);
        Assert.True(owner.TrySign(proof1, 60));
        Assert.True(owner.TrySign(proof2, 60));

        Assert.False(proof1.Equals(proof2));
        Assert.False(proof1.Equals((ContractableProof?)null));

        var copy = TinyhandSerializer.Deserialize<TestLinkageProof>(TinyhandSerializer.Serialize(proof1))!;
        Assert.True(proof1.Equals(copy));
    }

    [Fact]
    public void OwnerTokenEqualityComparesSignatureContent()
    {
        var token = OwnerToken.UnsafeConstructor();
        token.PublicKey = SeedKey.NewSignature().GetSignaturePublicKey();
        token.Signature = [1, 2, 3];
        token.SignedMics = 123;
        token.Salt = 456;

        var other = OwnerToken.UnsafeConstructor();
        other.PublicKey = token.PublicKey;
        other.Signature = [1, 2, 3];
        other.SignedMics = 123;
        other.Salt = 456;

        Assert.True(token.Equals(other));
        Assert.False(token.Equals(null));

        other.Signature = [1, 2, 4];
        Assert.False(token.Equals(other));

        other.Signature = [1, 2];
        Assert.False(token.Equals(other));
    }

    [Fact]
    public void OwnerTokenRequiresASignedTimestamp()
    {
        var token = OwnerToken.UnsafeConstructor();
        Assert.False(token.Validate());
        token.SignedMics = 1;
        Assert.True(token.Validate());
    }

    [Fact]
    public void EvidenceWithoutACreditIsRejected()
    {
        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var proof = new CredentialProof(publicKey, CredentialKind.Merger, new MergerState());
        Assert.True(key.TrySign(proof, 60));

        // A credential proof carries no credit, so the merger signatures cannot be checked.
        var evidence = new CredentialEvidence(proof);
        Assert.False(evidence.ValidateAndVerify());
        Assert.False(evidence.ValidateAndVerifyExceptProof(default));
        Assert.Null(evidence.GetSignature(0));
        Assert.Null(evidence.GetSignature(3));
        Assert.Null(evidence.GetSignature(-1));
    }

    [Fact]
    public void EvidenceDescribesItsProof()
    {
        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var value = new Value(publicKey, 1, new Credit(default, [publicKey]));
        var proof = new TestLinkageProof(publicKey, value);
        Assert.True(key.TrySign(proof, 60));

        var now = Mics.GetCorrected();
        var evidence = new ContractableEvidence(true, proof, proof, now, now + Mics.FromSeconds(60));
        Assert.Contains(publicKey.ToString(), evidence.ToString());
    }
}
