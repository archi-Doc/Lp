// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;

namespace Lp.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LpStats
{
    public const string Filename = "LpStats.tinyhand";

    [KeyAsName]
    public CredentialProof.GoshujinClass Credentials { get; private set; } = new();
}
