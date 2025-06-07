// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class MessageIdentity : Identity
{
    [Key(Identity.ReservedKeyCount + 0)]
    [MaxLength(64)]
    public required partial string Name { get; init; } = string.Empty;

    [Key(Identity.ReservedKeyCount + 1)]
    [MaxLength(64)]
    public required partial string Title { get; init; } = string.Empty;

    [Key(Identity.ReservedKeyCount + 2)]
    [MaxLength(256)]
    public required partial string Content { get; init; } = string.Empty;

    public MessageIdentity(Identifier sourceIdentifier, SignaturePublicKey originator)
        : base(sourceIdentifier, originator)
    {
    }
}
