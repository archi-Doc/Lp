// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CredentialProof : Proof
{// Credentials = CredentialProof.Goshujin
    public CredentialProof()
    {
    }

    #region FieldAndProperty

    [Key(Proof.ReservedKeyCount)]
    public Evidence ValueProofEvidence { get; private set; } = new(); // ValueProof

    [Key(Proof.ReservedKeyCount + 1)]
    public NetAddress NetAddress { get; private set; }

    #endregion

    public bool TryGetValueProof([MaybeNullWhen(false)] out ValueProof valueProof)
    {
        valueProof = this.ValueProofEvidence.Proof as ValueProof;
        return valueProof != null;
    }

    public override SignaturePublicKey GetPublicKey()
        => this.TryGetValueProof(out var valueProof) ? valueProof.GetPublicKey() : default;

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        if (this.TryGetValueProof(out var valueProof))
        {
            return valueProof.TryGetCredit(out credit);
        }
        else
        {
            credit = default;
            return false;
        }
    }

    public override bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        if (this.TryGetValueProof(out var valueProof))
        {
            return valueProof.TryGetValue(out value);
        }
        else
        {
            value = default;
            return false;
        }
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }
}
