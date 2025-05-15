// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ProofAndValue : Proof
{
    [Key(0)] // Key(0) is not used in the Proof class (reserved).
    public Value Value { get; protected set; } = default!;

    public override SignaturePublicKey GetSignatureKey()
        => this.Value.Owner;

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.Value.Credit;
        return true;
    }

    public override bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = this.Value;
        return true;
    }
}
