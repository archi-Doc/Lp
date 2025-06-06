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
    public static CreditColor NewBoard()
    {
        return new()
        {
            Openness = CreditOpenness.Open,
            Evol = CreditEvol.Unrestricted,
            Rule = CreditRule.Wild,
            MaxOwners = 1,
            CutoffPoint = 0,
        };
    }

    public CreditColor()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    public CreditOpenness Openness { get; init; }

    [Key(1)]
    public CreditEvol Evol { get; init; }

    [Key(2)]
    public CreditRule Rule { get; init; }

    [Key(3)]
    public int MaxOwners { get; init; }

    [Key(4)]
    public int CutoffPoint { get; init; }

    [Key(5)]
    public OwnerFee? OwnerFee { get; init; }

    [Key(6)]
    public OrderFee? OrderFee { get; init; }

    #endregion
}
