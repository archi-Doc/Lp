// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public class ProofUnionTest
{
    [Fact]
    public void EveryProofTypeIsRegisteredInTheProofUnion()
    {// Signing serializes a proof as Proof. An unregistered type is written as a constant, so its signature would not cover its content.
        var registered = typeof(Proof).GetCustomAttributes<TinyhandUnionAttribute>(false).Select(x => x.SubType).ToHashSet();
        var types = typeof(Proof).Assembly.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Proof)) && !x.IsAbstract && x.IsDefined(typeof(TinyhandObjectAttribute), false))
            .ToArray();

        Assert.Contains(typeof(MergerProof), types);
        Assert.All(types, x => Assert.True(registered.Contains(x), $"{x.Name} is not registered in the Proof union."));
    }

    [Fact]
    public void AMergerProofSignatureCoversTheProofContent()
    {
        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var signed = new MergerProof(publicKey);
        Assert.True(key.TrySign(signed, 60));
        Assert.True(signed.ValidateAndVerify());

        // Another proof with a copied signature must not be verified.
        var forged = new MergerProof(publicKey);
        Assert.True(forged.PrepareForSigning(ref publicKey, 3600));
        Assert.True(forged.SetSignature(new SignaturePair(0, signed.Signature)));
        Assert.False(forged.ValidateAndVerify());

        // A proof stored as Proof keeps its type and signature.
        var restored = TinyhandSerializer.DeserializeObject<Proof>(TinyhandSerializer.SerializeObject<Proof>(signed));
        Assert.IsType<MergerProof>(restored);
        Assert.True(restored.ValidateAndVerify());
    }

    [Fact]
    public void EvidenceForACreditWithoutMergersIsRejected()
    {// TestLinkageProof does not validate its value, so the evidence has to check the credit.
        var owner = SeedKey.NewSignature();
        var ownerKey = owner.GetSignaturePublicKey();
        var proof = new TestLinkageProof(ownerKey, new Value(ownerKey, 1, Credit.UnsafeConstructor()));
        Assert.True(owner.TrySign(proof, 60));
        Assert.True(proof.ValidateAndVerify());

        var now = Mics.GetCorrected();
        var evidence = new ContractableEvidence(true, proof, proof, now, now + Mics.FromSeconds(60));
        Assert.False(evidence.ValidateAndVerify()); // No merger signature would be required otherwise.
    }
}
