// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc;
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
    public void TestDomain()
    {
        var domain = new DomainIdentifier(LpConstants.LpCredit, this.testNode, string.Empty);
        var st = domain.ConvertToString();
        DomainIdentifier.TryParse(st, out var domain2, out var read).IsTrue();
        domain2!.Equals(domain).IsTrue();

        domain = new DomainIdentifier(LpConstants.LpCredit, this.testNode, "test");
        st = domain.ConvertToString();
        DomainIdentifier.TryParse(st, out domain2, out read).IsTrue();
        domain2!.Equals(domain).IsTrue();
    }

    [Fact]
    public void Test1()
    {
        Mics.UpdateFastCorrected();
        var validitySeconds = Seconds.FromDays(1);

        var ownerKey = SeedKey.NewSignature();
        var owner = ownerKey.GetSignaturePublicKey();
        var ownerKey2 = SeedKey.NewSignature();
        var owner2 = ownerKey2.GetSignaturePublicKey();
        var mergerKey = SeedKey.NewSignature();
        var merger = mergerKey.GetSignaturePublicKey();
        var linkerKey = SeedKey.NewSignature();
        var linker = linkerKey.GetSignaturePublicKey();

        var creditIdentity = new CreditIdentity(default, owner, [merger,]);
        Credit.TryCreate(creditIdentity, out var credit).IsTrue();
        var value = new Value(owner, 1, credit!); // owner2#2@credit
        var value2 = new Value(owner2, 2, credit!); // owner2#2@credit
        var state = new MergerState();
        state.NetNode = this.testNode;
        state.Name = "Test1";

        var credentialProof = new CredentialProof(owner, CredentialKind.Merger, state);
        ownerKey.TrySign(credentialProof, validitySeconds).IsTrue();
        credentialProof.ValidateAndVerify().IsTrue();

        mergerKey.TrySign(credentialProof, validitySeconds).IsTrue();
        credentialProof.ValidateAndVerify().IsTrue();

        var linkageProof = new TestLinkageProof(linker, value);
        ownerKey.TrySign(linkageProof, validitySeconds).IsTrue();
        linkageProof.ValidateAndVerify().IsTrue();
        var linkageProof2 = new TestLinkageProof(linker, value2);
        ownerKey2.TrySign(linkageProof2, validitySeconds).IsTrue();
        linkageProof2.ValidateAndVerify().IsTrue();
        var linkedMics = Mics.FastCorrected;
        var expirationMics = Mics.FastCorrected + Mics.FromSeconds(10);

        var linkageEvidence = new ContractableEvidence(true, linkageProof, linkageProof2, linkedMics, expirationMics);
        mergerKey.TrySign(linkageEvidence, 0).IsTrue();
        linkageEvidence.ValidateAndVerify().IsTrue();

        var linkageEvidence2 = new ContractableEvidence(false, linkageProof, linkageProof2, linkedMics, expirationMics);
        mergerKey.TrySign(linkageEvidence2, 0).IsTrue();
        linkageEvidence2.ValidateAndVerify().IsTrue();

        Linkage.TryCreate(linkageEvidence, linkageEvidence2, out var linkage).IsTrue();
        linkerKey.TrySign(linkage!, validitySeconds).IsTrue();

        linkage!.ValidateAndVerify().IsTrue();

        var bin = TinyhandSerializer.Serialize(linkage);
        var linkage2 = TinyhandSerializer.Deserialize<Linkage>(bin);
        linkage2!.ValidateAndVerify().IsTrue();
        bin.SequenceEqual(TinyhandSerializer.Serialize(linkage2)).IsTrue();

        linkage.StripProof(ref owner);

        linkage!.ValidateAndVerify().IsTrue();

        bin = TinyhandSerializer.Serialize(linkage);
        linkage2 = TinyhandSerializer.Deserialize<Linkage>(bin);
        linkage2!.ValidateAndVerify().IsTrue();
        bin.SequenceEqual(TinyhandSerializer.Serialize(linkage2)).IsTrue();

        linkage.StripProof(ref owner2);

        linkage!.ValidateAndVerify().IsFalse();

        bin = TinyhandSerializer.Serialize(linkage);
        linkage2 = TinyhandSerializer.Deserialize<Linkage>(bin);
        bin.SequenceEqual(TinyhandSerializer.Serialize(linkage2)).IsTrue();
    }
}
