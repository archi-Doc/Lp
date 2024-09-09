// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Tinyhand.IO;

namespace Lp.T3cs;

/// <summary>
/// Immutable evidence object.
/// </summary>
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class Evidence : IValidatable
{
    public static bool TryCreate(Proof proof, [MaybeNullWhen(false)] out Evidence evidence)
    {
        if (!proof.TryGetCredit(out var credit))
        {
            evidence = default;
            return false;
        }

        var obj = new Evidence();
        obj.Proof = proof;

        evidence = obj;
        return true;
    }

    [Link(Primary = true, TargetMember = "ProofMics", Type = ChainType.Ordered)]
    public Evidence()
    {
        this.Proof = default!;
    }

    [Key(0)]
    public Proof Proof { get; private set; }

    [Key(1, Level = TinyhandWriter.DefaultSignatureLevel + 100)]
    public Evidence? Engagement { get; private set; }

    [Key(2, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature0 { get; private set; }

    [Key(3, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature1 { get; private set; }

    [Key(4, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature2 { get; private set; }

    public long ProofMics
        => this.Proof.VerificationMics;

    public bool Validate()
    {
        return this.Proof.Validate();
    }
}
