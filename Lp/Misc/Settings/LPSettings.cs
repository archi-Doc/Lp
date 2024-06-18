// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LPSettings
{
    public const string Filename = "Settings.tinyhand";

    public LPFlags Flags { get; set; } = default!;
}
