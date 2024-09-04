// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

/*
/// <summary>
/// Represents a linkage address.
/// </summary>
[TinyhandObject]
public sealed partial class LinkageAddress // : IValidatable, IEquatable<LinkageAddress>
{
    public LinkageAddress()
    {
    }

    public LinkageAddress(LinkageKey key, string creditName)
    {
        this.Key = key;
        this.CreditName = creditName;
    }

    public LinkageAddress(LinkageKey key, Credit credit)
    {
        this.Key = key;
        this.Credit = credit;
    }

    #region FieldAndProperty

    [Key(0)]
    public LinkageKey Key { get; private set; }

    [Key(1)]
    public string? CreditName { get; private set; }

    [Key(2)]
    public Credit? Credit { get; private set; }

    #endregion

    public bool IsValid
        => this.CreditName is not null || this.Credit is not null;

    public override string ToString()
    {
        var key = this.Key.ToString();
        if (this.CreditName is not null)
        {
            return $"{key}{Credit.CreditSymbol}{this.CreditName}";
        }
        else if (this.Credit is not null)
        {
            return $"{key}{Credit.CreditSymbol}{this.Credit.ToString()}";
        }
        else
        {
            return $"{key}{Credit.CreditSymbol}";
        }
    }
}*/
