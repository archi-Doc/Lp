// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
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

        var creditIdentity = new Identity(IdentityKind.Credit, owner, [merger,]);
        Credit.TryCreate(creditIdentity, out var credit).IsTrue();
        var value = new Value(owner, 2, credit!);
        var state = new MergerState();
        state.NetNode = this.testNode;
        state.Name = "Test1";

        var credentialProof = new CredentialProof(value, CredentialKind.Merger, state);
        ownerKey.TrySign(credentialProof, validMics).IsTrue();
        credentialProof.ValidateAndVerify().IsFalse();

        mergerKey.TrySign(credentialProof, validMics).IsTrue();
        credentialProof.ValidateAndVerify().IsTrue();

        //var linkageProof = new ProofWithLinker
        //var linkageEvidence = new LinkageEvidence(credentialProof, owner, 4);
    }
}
