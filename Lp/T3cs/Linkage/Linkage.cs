// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class LinkerProof : ProofAndPublicKey
{
    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }
}

/*[TinyhandObject]
public partial class LinkerLinkage : Linkage<LinkerProof>
{
}*/

[TinyhandObject]
public partial class Linkage<TProof> : IValidatable
    where TProof : Proof
{
    #region FieldAndProperty

    [Key(0)]
    public TProof ProofA { get; set; }

    [Key(1)]
    public TProof ProofB { get; set; }

    [Key(2)]
    private byte[]? mergerSignatureA1;

    [Key(3)]
    private byte[]? mergerSignatureA2;

    [Key(4)]
    private byte[]? mergerSignatureA3;

    [Key(5)]
    private byte[]? mergerSignatureB1;

    [Key(6)]
    private byte[]? mergerSignatureB2;

    [Key(7)]
    private byte[]? mergerSignatureB3;

    [Key(8)]
    private byte[]? linkerSignature;

    #endregion

    public Linkage(TProof proofA, TProof proofB)
    {
        this.ProofA = proofA;
        this.ProofB = proofB;
    }

    public bool Validate()
    {
        SignaturePublicKey linkerPublicKey;
        if (!this.ProofA.Validate() ||
            !this.ProofA.TryGetLinkerPublicKey(out linkerPublicKey) ||
            !linkerPublicKey.IsValid)
        {
            return false;
        }

        if (!this.ProofB.Validate() ||
            !this.ProofB.TryGetLinkerPublicKey(out linkerPublicKey) ||
            !linkerPublicKey.IsValid)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Immutable linkage object (authentication between mergers).
/// </summary>
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class Linkage : IValidatable
{// Linkage x Point
    // [Link(Primary = true, TargetMember = "ProofMics", Type = ChainType.Ordered)]
    public Linkage(Evidence linkedEvidence)
    {
        // this.LinkedEvidence = linkedEvidence;
    }

    private Linkage()
    {
        // this.LinkedEvidence = default!;
    }

    // [Key(0)]
    // public Evidence LinkedEvidence { get; private set; }

    [Key(1)]
    public byte[]? WatchmanSignature { get; private set; }

    public bool Validate()
    {
        return true;
        // return this.LinkedEvidence.Validate();
    }
}
