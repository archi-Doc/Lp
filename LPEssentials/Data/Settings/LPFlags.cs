// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LPFlags
{
    public bool LogEssentialNetMachine { get; set; } // EssentialNetMachine
}
