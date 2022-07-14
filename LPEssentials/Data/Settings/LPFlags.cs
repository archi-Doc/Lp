// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LPFlags
{
    [ShortName("logm1")]
    public bool LogEssentialNetMachine { get; set; } // EssentialNetMachine
}
