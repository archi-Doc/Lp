// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class ZenItzOptions
{
    [SimpleOption("zenfile", null, ".")]
    public string ZenFile { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"ZenItz Options";
    }
}
