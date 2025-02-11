// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

/// <summary>
/// Immutable linkage object (authentication between mergers).
/// </summary>
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class Linkage : IValidatable
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
