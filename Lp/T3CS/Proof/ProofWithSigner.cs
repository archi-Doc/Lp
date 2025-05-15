// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// The general Proof class only supports authentication using the target <see cref="SignaturePublicKey"/>,<br/>
/// but this class supports authentication using the target PublicKey, Mergers, and LpKey.<br/>
/// The authentication key is determined as follows: <br/>
/// If Signer is 0, the target PublicKey is used;<br/>
/// if it is between 1 and MergerCount, a Merger is used;<br/>
/// otherwise, LpKey is used.
/// </summary>
public abstract partial class ProofWithSigner : Proof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Proof.ReservedKeyCount + 2;

    [Key(Proof.ReservedKeyCount)]
    public Value Value { get; protected set; } = default!;

    /// <summary>
    /// Gets the signer index indicating which key is used for authentication.<br/>
    /// If <c>0</c>, the target <see cref="Value.Owner"/> is used.<br/>
    /// If between <c>1</c> and <c>Value.Credit.MergerCount</c>, a merger key is used.<br/>
    /// Otherwise, the <see cref="LpConstants.LpPublicKey"/> is used.
    [Key(Proof.ReservedKeyCount + 1)]
    public int Signer { get; private set; }

    public override SignaturePublicKey GetSignatureKey()
    {
        if (this.Signer == 0)
        {
            return this.Value.Owner;
        }
        else if (this.Signer > 0 && this.Signer <= this.Value.Credit.MergerCount)
        {
            return this.Value.Credit.Mergers[this.Signer - 1];
        }

        return LpConstants.LpPublicKey;
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
