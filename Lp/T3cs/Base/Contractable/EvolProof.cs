// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class EvolProof : ContractableProof
{
    [Key(ContractableProof.ReservedKeyCount + 0)]
    public Value SourceValue { get; protected set; } = default!;

    [Key(ContractableProof.ReservedKeyCount + 1)]
    public Value TargetValue { get; protected set; } = Value.UnsafeConstructor();

    [Key(ContractableProof.ReservedKeyCount + 2)]
    public Identity? TargetIdentity { get; protected set; }

    public EvolProof(SignaturePublicKey linkerPublicKey, Value sourceValue, Value targetValue, Identity? targetIdentity)
        : base(linkerPublicKey)
    {
        this.SourceValue = sourceValue;
        this.TargetValue = targetValue;
        this.TargetIdentity = targetIdentity;
    }

    public override SignaturePublicKey GetSignatureKey() => this.SourceValue.Owner;

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.SourceValue.Credit;
        return true;
    }

    public override bool PrepareForSigning(ref SignaturePublicKey publicKey, long validMics)
    {
        if (!this.SourceValue.Owner.Equals(ref publicKey))
        {
            return false;
        }

        return base.PrepareForSigning(ref publicKey, validMics);
    }
}
