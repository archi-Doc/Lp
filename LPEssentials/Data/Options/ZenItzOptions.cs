// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class ZenItzOptions : ILogInformation
{
    [SimpleOption("zenfile", null, ".")]
    public string ZenFile { get; set; } = string.Empty;

    public void LogInformation(ILog logger)
    {
    }
}
