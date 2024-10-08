// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class ConstraintsAndCovenants
{
    public ConstraintsAndCovenants()
    {
    }

    [Key(0)]
    public Credit SourceCredit { get; private set; } = new();

    [Key(1)]
    public bool IsUnlinkable { get; private set; } = false;
}
