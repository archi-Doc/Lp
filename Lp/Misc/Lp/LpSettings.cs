// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Data;

[TinyhandObject(ImplicitMemberNameAsKey = true)]
public partial record LpSettings
{
    public const string Filename = "Settings.tinyhand";

    public LpFlags Flags { get; set; } = new();

    public ColorClass Color { get; set; } = new();

    [TinyhandObject(ImplicitMemberNameAsKey = true, EnumAsString = true, SkipDefaultValues = false)]
    public partial record ColorClass
    {
        public ConsoleColor Default { get; set; } = ConsoleColor.White;

        public ConsoleColor Warning { get; set; } = ConsoleColor.Yellow;

        public ConsoleColor Error { get; set; } = ConsoleColor.Red;
    }
}
