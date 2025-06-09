// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ContractableProofWithValue : ContractableProof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = ContractableProof.ReservedKeyCount + 1;

    [Key(ContractableProof.ReservedKeyCount)]
    public Value Value { get; protected set; } = default!;

    public ContractableProofWithValue(SignaturePublicKey linkerPublicKey, Value value)
        : base(linkerPublicKey)
    {
        this.Value = value;
    }

    public override SignaturePublicKey GetSignatureKey() => this.Value.Owner;

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

    public override bool PrepareForSigning(ref SignaturePublicKey publicKey, long validMics)
    {
        if (!this.Value.Owner.Equals(ref publicKey))
        {
            return false;
        }

        return base.PrepareForSigning(ref publicKey, validMics);
    }
}
