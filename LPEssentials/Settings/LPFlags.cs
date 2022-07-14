// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Options;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LPFlags
{
    public bool LogENM { get; set; } // EssentialNetMachine
}
