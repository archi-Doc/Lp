// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class Linkage<TProof>
    where TProof : Proof
{
    private Linkage()
    {
    }

    [Key(0)]
    public Proof ProofA { get; set; } = default!;

    [Key(1)]
    public Proof ProofB { get; set; } = default!;

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
