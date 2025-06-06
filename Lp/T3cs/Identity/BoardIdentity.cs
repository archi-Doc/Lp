// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial class BoardIdentity : Identity
{
    [Key(4)]
    [MaxLength(64)]
    public required partial string Name { get; init; } = string.Empty;
}
