// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true)]
public partial class MergedProof : Proof
{
    [Key(Proof.ReservedKeyCount)]
    public Value Value { get; protected set; } = default!;

    [Key(Proof.ReservedKeyCount + 1)]
    public byte MergerIndex { get; protected set; } = default!;

    public MergedProof(Value value)
    {
        this.Value = value;
    }

    public override SignaturePublicKey GetSignatureKey()
        => this.Value.Credit.GetMerger(this.MergerIndex);

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
        var mergerIndex = this.Value.Credit.GetMergerIndex(ref publicKey);
        if (mergerIndex < 0)
        {
            return false;
        }

        this.MergerIndex = (byte)mergerIndex;
        return base.PrepareForSigning(ref publicKey, validitySeconds);
    }

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (this.MergerIndex < 0 ||
            this.MergerIndex >= LpConstants.MaxMergers)
        {
            return false;
        }

        if (!base.Validate(validationOptions))
        {
            return false;
        }

        return true;
    }
}
