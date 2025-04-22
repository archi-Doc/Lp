// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CreditInformation
{
    public static readonly CreditInformation Default = new();

    public CreditInformation()
    {
    }

    // [Key(0)]
    // public Credit Credit { get; private set; } = Credit.Default;

    [Key(0)]
    public string CreditName { get; private set; } = string.Empty;

    [Key(1)]
    public CreditColor CreditPolicy { get; private set; } = CreditColor.Default;
}
