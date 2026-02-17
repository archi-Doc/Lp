// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true)]
public partial class MergedProof : Proof
{
    [Key(Proof.ReservedKeyCount)]
    public Value Value { get; protected set; } = default!;

    public MergedProof(Value value)
    {
        this.Value = value;
    }

    public override SignaturePublicKey GetSignatureKey() => this.Value.Credit.PrimaryMerger;

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.Value.Credit;
        return true;
    }

    /*public override bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = this.Value;
        return true;
    }*/

    public override bool PrepareForSigning(ref SignaturePublicKey publicKey, int validitySeconds)
    {
        if (!this.Value.Owner.Equals(ref publicKey))
        {
            return false;
        }

        return base.PrepareForSigning(ref publicKey, validitySeconds);
    }

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        return true;
    }
}
