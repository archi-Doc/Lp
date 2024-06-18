// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public enum OrderFeeType
{
    Constant,
    Ratio,
    ConstantOrRatio,
}

/// <summary>
/// Represents an owner fee.
/// </summary>
[TinyhandObject]
public sealed partial class OrderFee : IEquatable<OrderFee>
{
    public static readonly OrderFee Default = new() { Type = OwnerFeeType.Constant, ConstantPerOrder = 1, };

    public OrderFee()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    public OwnerFeeType Type { get; private set; }

    [Key(1)]
    public long ConstantPerOrder { get; private set; }

    [Key(2)]
    public double RatioPerOrder { get; private set; }

    #endregion

    public bool Equals(OrderFee? other)
    {
        if (other is null)
        {
            return false;
        }
        else if (this.Type != other.Type)
        {
            return false;
        }
        else if (this.ConstantPerOrder != other.ConstantPerOrder)
        {
            return false;
        }
        else if (this.RatioPerOrder != other.RatioPerOrder)
        {
            return false;
        }

        return true;
    }
}
