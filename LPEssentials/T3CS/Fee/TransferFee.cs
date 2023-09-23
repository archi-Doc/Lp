// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

[TinyhandObject]
public partial record TransferFee
{
    public TransferFee()
    {
    }

    public TransferFee(Int128 minimumFee, Int128 fixedFee, double feeRatio)
    {
        this.MinimumFee = minimumFee;
        this.FixedFee = fixedFee;
        this.FeeRatio = feeRatio;
    }

    [Key(0)]
    public Int128 MinimumFee { get; protected set; }

    [Key(1)]
    public Int128 FixedFee { get; protected set; }

    [Key(2)]
    public double FeeRatio { get; protected set; }

    public Int128 CalculateFee(Int128 point)
    {
        var fee = this.FixedFee + (point.ToDouble() * this.FeeRatio).ToInt128();
        return fee < this.MinimumFee ? this.MinimumFee : fee;
    }
}
