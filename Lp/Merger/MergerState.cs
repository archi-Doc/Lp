// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace Lp;

[TinyhandObject]
public partial record MergerState
{
    [Key(0)]
    public NetNode Node { get; private set; } = new();

    public MergerState()
    {
    }
}
