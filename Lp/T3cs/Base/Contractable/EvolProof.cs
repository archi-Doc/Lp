// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class EvolProof : ContractableProof
{
    [Key(ContractableProof.ReservedKeyCount + 0)]
    public Value SourceValue { get; protected set; }

    [Key(ContractableProof.ReservedKeyCount + 1)]
    public Value DestinationValue { get; protected set; }

    [Key(ContractableProof.ReservedKeyCount + 2)]
    public Identity? DestinationIdentity { get; protected set; }

    public EvolProof(SignaturePublicKey linkerPublicKey, Value sourceValue, Value destinationValue, Identity? destinationIdentity)
        : base(linkerPublicKey)
    {
        this.SourceValue = sourceValue;
        this.DestinationValue = destinationValue;
        this.DestinationIdentity = destinationIdentity;
    }

    public override SignaturePublicKey GetSignatureKey() => this.SourceValue.Owner;

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        if (!this.SourceValue.Validate() || !this.DestinationValue.Validate())
        {
            return false;
        }

        if (this.DestinationIdentity is not null)
        {
            var identifier = this.DestinationIdentity.GetIdentifier();
            if (!this.DestinationValue.Credit.Identifier.Equals(ref identifier))
            {
                return false;
            }
        }

        return true;
    }

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
