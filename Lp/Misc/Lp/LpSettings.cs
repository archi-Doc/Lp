// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Data;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial record LpSettings
{
    public const string Filename = "Settings.tinyhand";

    public LpFlags Flags { get; set; } = default!;
}
