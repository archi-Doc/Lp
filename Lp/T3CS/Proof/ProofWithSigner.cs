// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ProofWithSigner : Proof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Proof.ReservedKeyCount + 2;

    [Key(Proof.ReservedKeyCount)]
    public Value Value { get; protected set; } = default!;

    [Key(Proof.ReservedKeyCount + 1)]
    public int SignerIndex { get; private set; }

    public override SignaturePublicKey GetSignatureKey()
    {
        if (this.SignerIndex >= 0 && this.SignerIndex < this.Value.Credit.MergerCount)
        {
            return this.Value.Credit.Mergers[this.SignerIndex];
        }

        return this.Value.Owner;
    }

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
