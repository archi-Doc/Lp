// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Data;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial record LpSettings
{
    public const string Filename = "Settings.tinyhand";

    public LpFlags Flags { get; set; } = default!;

    public ColorClass Color { get; set; } = new();

    [TinyhandObject(ImplicitMemberNameAsKey = true, EnumAsString = true)]
    public partial record ColorClass
    {
        public ConsoleColor Information { get; set; } = ConsoleColor.White;

        public ConsoleColor Error { get; set; } = ConsoleColor.Red;
    }
}
