// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using Arc.Crypto;
using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace xUnitTest;

public class LinkageSignatureTest
{
    private static readonly FieldInfo LinkerSignatureField = typeof(Linkage).GetField("linkerSignature", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Fact]
    public void DerivedLinkagesAreVerifiedWithTheLinkerSignature()
    {
        var (evidence1, evidence2, linker) = CreateEvidences();

        Assert.True(AccountableLinkage.TryCreate(evidence1, evidence2, out var accountable));
        Assert.True(linker.TrySign(accountable));
        Assert.True(accountable.ValidateAndVerify());

        Assert.True(LinkLinkage.TryCreate(evidence1, evidence2, out var link));
        Assert.True(linker.TrySign(link));
        Assert.True(link.ValidateAndVerify());
    }

    [Fact]
    public void ALinkerSignatureIsBoundToTheLinkageType()
    {
        var (evidence1, evidence2, linker) = CreateEvidences();
        Assert.True(AccountableLinkage.TryCreate(evidence1, evidence2, out var accountable));
        Assert.True(LinkLinkage.TryCreate(evidence1, evidence2, out var link));
        Assert.True(linker.TrySign(accountable));

        link.SetSignInternal((byte[])LinkerSignatureField.GetValue(accountable)!);
        Assert.False(link.ValidateAndVerify());
    }

    [Fact]
    public void SignaturesOnBaseLinkagesAreUnchanged()
    {// Reproduce the previous signing (the static Linkage serializer at SignatureLevel - 1).
        var (evidence1, evidence2, linker) = CreateEvidences();
        Assert.True(Linkage.TryCreate(evidence1, evidence2, out var linkage));

        var signature = new byte[CryptoSign.SignatureSize];
        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = Linkage.SignatureLevel - 1;
        try
        {
            TinyhandSerializer.SerializeObject<Linkage>(ref writer, linkage, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            linker.Sign(span, signature);
        }
        finally
        {
            writer.Dispose();
        }

        linkage.SetSignInternal(signature);
        Assert.True(linkage.ValidateAndVerify());

        Assert.True(linker.TrySign(linkage));
        Assert.Equal(signature, (byte[])LinkerSignatureField.GetValue(linkage)!); // Ed25519 signatures are deterministic.
    }

    [Fact]
    public void AClonedLinkageRemainsValid()
    {
        var (evidence1, evidence2, linker) = CreateEvidences();
        Assert.True(LinkLinkage.TryCreate(evidence1, evidence2, out var link));
        Assert.True(linker.TrySign(link));

        var clone = TinyhandSerializer.CloneObject(link);
        Assert.NotNull(clone);
        Assert.True(clone.Contract1.IsProof);
        Assert.True(clone.Contract2.IsProof);
        Assert.True(clone.ValidateAndVerify());
    }

    private static (ContractableEvidence Evidence1, ContractableEvidence Evidence2, SeedKey Linker) CreateEvidences()
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
        return (evidence1, evidence2, linker);
    }
}
