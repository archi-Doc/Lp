// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class DimensionProof : ProofWithPublicKey
{
    private const double MinDimension = -1d;
    private const double MaxDimension = 1d;

    public DimensionProof(SignaturePublicKey publicKey)
        : base(publicKey)
    {
    }

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public double Dimension { get; private set; }

    // public Value Value { get; private set; } = new();

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        if (this.Dimension < MinDimension ||
            this.Dimension > MaxDimension)
        {
            return false;
        }

        return true;
    }
}
