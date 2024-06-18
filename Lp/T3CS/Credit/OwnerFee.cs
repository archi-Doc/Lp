// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public enum OwnerFeeType
{
    Constant,
    Ratio,
    ConstantOrRatio,
}

/// <summary>
/// Represents an owner fee.
/// </summary>
[TinyhandObject]
public sealed partial class OwnerFee : IEquatable<OwnerFee>
{
    public static readonly OwnerFee Default = new() { Type = OwnerFeeType.Constant, ConstantPerMonth = 1, };

    public OwnerFee()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    public OwnerFeeType Type { get; private set; }

    [Key(1)]
    public long ConstantPerMonth { get; private set; }

    [Key(2)]
    public double RatioPerMonth { get; private set; }

    #endregion

    public bool Equals(OwnerFee? other)
    {
        if (other is null)
        {
            return false;
        }
        else if (this.Type != other.Type)
        {
            return false;
        }
        else if (this.ConstantPerMonth != other.ConstantPerMonth)
        {
            return false;
        }
        else if (this.RatioPerMonth != other.RatioPerMonth)
        {
            return false;
        }

        return true;
    }
}
