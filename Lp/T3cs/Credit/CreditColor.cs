// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public enum CreditOpenness
{
    Open,
    Partial,
    Closed,
}

public enum CreditEvol
{
    Unrestricted,
    Exchange,
    Constant,
    Linear,
    Ratio,
}

public enum CreditRule
{
    Wild,
    Civilized,
}

/// <summary>
/// Represents a credit color.
/// </summary>
[TinyhandObject]
public sealed partial record CreditColor
{
    public static readonly CreditColor Default = new();

    public CreditColor()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    public CreditOpenness Openness { get; private set; }

    [Key(1)]
    public CreditEvol Evol { get; private set; }

    [Key(2)]
    public CreditRule Rule { get; private set; }

    [Key(3)]
    public int MaxOwners { get; private set; }

    [Key(4)]
    public int CutoffPoint { get; private set; }

    [Key(5)]
    public OwnerFee OwnerFee { get; private set; } = OwnerFee.Default;

    [Key(6)]
    public OrderFee OrderFee { get; private set; } = OrderFee.Default;

    #endregion
}
