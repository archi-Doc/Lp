﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

/*public abstract partial class ProofAndCredit : Proof
{
    [Key(0)] // Key(0) is not used in the Proof class (reserved).
    public Credit Credit { get; protected set; } = new();

    public override SignaturePublicKey GetPublicKey()
        => this.Credit.Originator;

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.Credit;
        return true;
    }
}*/
