// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Data;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial record LpFlags
{
    [ShortName("logm1")]
    public bool LogEssentialNetMachine { get; set; }
}
