// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

/// <summary>
/// Immutable linkage object.
/// </summary>
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class Linkage : IValidatable
{// Linkage x Point
    // [Link(Primary = true, TargetMember = "ProofMics", Type = ChainType.Ordered)]
    public Linkage(Proof proof0, Proof proof1)
    {
        this.Proof0 = proof0;
        this.Proof1 = proof1;
    }

    private Linkage()
    {
        this.Proof0 = default!;
        this.Proof1 = default!;
    }

    [Key(0)]
    public Proof Proof0 { get; private set; }

    [Key(1)]
    public Proof Proof1 { get; private set; }

    [Key(2, Level = 0)]
    public byte[]? MergerSignature0 { get; private set; }

    [Key(3, Level = 1)]
    public byte[]? MergerSignature1 { get; private set; }

    [Key(4, Level = 2)]
    public byte[]? MergerSignature2 { get; private set; }

    public bool Validate()
    {
        return this.Proof0.Validate() && this.Proof1.Validate();
    }
}
