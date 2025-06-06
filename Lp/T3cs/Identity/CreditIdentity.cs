// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class CreditIdentity : Identity
{
    [SetsRequiredMembers]
    public CreditIdentity(IdentityKind identityKind, SignaturePublicKey originator, SignaturePublicKey[] mergers)
        : base(identityKind, originator, mergers)
    {
    }
}
