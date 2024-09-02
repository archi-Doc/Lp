// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial class DimensionProof : ProofAndPublicKey
{
    private const double MinDimension = -1d;
    private const double MaxDimension = 1d;

    public DimensionProof()
    {
    }

    [Key(5)]
    public double Dimension { get; private set; }

    public Value Value { get; private set; } = new();//

    public bool ValidateAndVerify()
    {
        return LpHelper.ValidateAndVerify(this);
    }

    public override bool Validate()
    {
        if (!base.Validate())
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
