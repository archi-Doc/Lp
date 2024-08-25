﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

/// <summary>
/// Immutable evidence object.
/// </summary>
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class Evidence : IValidatable
{
    [Link(Primary = true, TargetMember = "ProofMics", Type = ChainType.Ordered)]
    public Evidence(Proof proof)
    {
        this.Proof = proof;
    }

    private Evidence()
    {
        this.Proof = default!;
    }

    [Key(0)]
    public Proof Proof { get; private set; }

    [Key(1, Level = 0)]
    public byte[]? MergerSignature0 { get; private set; }

    [Key(2, Level = 1)]
    public byte[]? MergerSignature1 { get; private set; }

    [Key(3, Level = 2)]
    public byte[]? MergerSignature2 { get; private set; }

    public long ProofMics
        => this.Proof.ProofMics;

    public bool Validate()
    {
        return this.Proof.Validate();
    }
}
