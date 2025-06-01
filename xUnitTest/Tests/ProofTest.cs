// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public class ProofTest
{
    private NetNode testNode;

    public ProofTest()
    {
        NetNode.TryParse("12.34.56.78:4567[1111:2222:3333:4444:5555:6666:7777:8888]:5678(e:hRp62w_fsJ9YeLVyyrXnPPilxUOKGePvaYsqJtE8GJenll5C)", out var node, out _);
        this.testNode = node!;
    }

    [Fact]
    public void Test1()
    {
        Mics.UpdateFastCorrected();
        var validMics = Mics.FromDays(1);

        var ownerKey = SeedKey.NewSignature();
        var owner = ownerKey.GetSignaturePublicKey();
        var mergerKey = SeedKey.NewSignature();
        var merger = mergerKey.GetSignaturePublicKey();
        var linkerKey = SeedKey.NewSignature();
        var linker = linkerKey.GetSignaturePublicKey();

        var creditIdentity = new Identity(IdentityKind.Credit, owner, [merger,]);
        Credit.TryCreate(creditIdentity, out var credit).IsTrue();
        var value = new Value(owner, 2, credit!); // owner#2@credit
        var state = new MergerState();
        state.NetNode = this.testNode;
        state.Name = "Test1";

        var credentialProof = new CredentialProof(value, CredentialKind.Merger, state);
        ownerKey.TrySign(credentialProof, validMics).IsTrue();
        credentialProof.ValidateAndVerify().IsTrue();

        mergerKey.TrySign(credentialProof, validMics).IsTrue();
        credentialProof.ValidateAndVerify().IsTrue();

        var linkageProof = new TestLinkageProof(value, linker);
        ownerKey.TrySign(linkageProof, validMics).IsTrue();
        var linkageProof2 = new TestLinkageProof(value, linker);
        ownerKey.TrySign(linkageProof2, validMics).IsTrue();
        var linkedMics = Mics.FastCorrected;

        var linkageEvidence = new ContractableEvidence(true, linkedMics, linkageProof, linkageProof2);
        mergerKey.TrySign(linkageEvidence, 0).IsTrue();
        linkageEvidence.ValidateAndVerify().IsTrue();

        var linkageEvidence2 = new ContractableEvidence(false, linkedMics, linkageProof, linkageProof2);
        mergerKey.TrySign(linkageEvidence2, 0).IsTrue();
        linkageEvidence2.ValidateAndVerify().IsTrue();

        Linkage2.TryCreate(linkageEvidence, linkageEvidence2, out var linkage).IsTrue();
        linkerKey.TrySign(linkage!, validMics).IsTrue();

        linkage!.ValidateAndVerify().IsTrue();

        var bin = TinyhandSerializer.Serialize(linkage);
        var linkage2 = TinyhandSerializer.Deserialize<Linkage2>(bin);
        linkage2!.ValidateAndVerify().IsTrue();
        bin.SequenceEqual(TinyhandSerializer.Serialize(linkage2)).IsTrue();

        linkage.Remove(ref owner);

        linkage!.ValidateAndVerify().IsTrue();

        bin = TinyhandSerializer.Serialize(linkage);
        linkage2 = TinyhandSerializer.Deserialize<Linkage2>(bin);
        linkage2!.ValidateAndVerify().IsTrue();
        bin.SequenceEqual(TinyhandSerializer.Serialize(linkage2)).IsTrue();

        linkage.Remove(ref linker);

        linkage!.ValidateAndVerify().IsTrue();

        bin = TinyhandSerializer.Serialize(linkage);
        linkage2 = TinyhandSerializer.Deserialize<Linkage2>(bin);
        linkage2!.ValidateAndVerify().IsTrue();
        bin.SequenceEqual(TinyhandSerializer.Serialize(linkage2)).IsTrue();

        linkage.Remove(ref linker);

        linkage!.ValidateAndVerify().IsFalse();

        bin = TinyhandSerializer.Serialize(linkage);
        linkage2 = TinyhandSerializer.Deserialize<Linkage2>(bin);
        bin.SequenceEqual(TinyhandSerializer.Serialize(linkage2)).IsTrue();
    }
}
