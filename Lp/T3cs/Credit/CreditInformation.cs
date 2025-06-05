// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CreditInformation
{
    public CreditInformation(string creditName, CreditColor creditColor)
    {
    }

    // [Key(0)]
    // public Credit Credit { get; private set; } = Credit.Default;

    [Key(0)]
    public string CreditName { get; private set; } = string.Empty;

    [Key(1)]
    public CreditColor CreditColor { get; private set; } = new();
}
