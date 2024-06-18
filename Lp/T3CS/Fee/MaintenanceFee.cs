// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial record MaintenanceFee : TransferFee
{
    public MaintenanceFee()
    {
    }

    public MaintenanceFee(long intervalMics, Int128 minimumFee, Int128 fixedFee, double feeRatio)
        : base(minimumFee, fixedFee, feeRatio)
    {
        this.IntervalMics = intervalMics;
    }

    [Key(3)]
    public long IntervalMics { get; protected set; }
}
