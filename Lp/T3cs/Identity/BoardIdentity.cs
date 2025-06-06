// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class BoardIdentity : Identity
{
    [Key(Identity.ReservedKeyCount + 0)]
    [MaxLength(64)]
    public required partial string Name { get; init; } = string.Empty;

    public BoardIdentity(Identifier sourceIdentifier, SignaturePublicKey originator)
        : base(sourceIdentifier, originator)
    {
    }
}
