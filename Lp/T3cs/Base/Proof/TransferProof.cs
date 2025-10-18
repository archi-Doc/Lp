// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class TransferProof : ProofWithValue
{
    public TransferProof(Value value)
        : base(value)
    {
    }

    [Key(ProofWithValue.ReservedKeyCount + 0)]
    public SignaturePublicKey RecipientKey { get; protected set; }
}

[TinyhandObject]
public partial class TransferProof2 : Proof
{
    [Key(Proof.ReservedKeyCount + 0)]
    public Credit Credit { get; protected set; }

    [Key(Proof.ReservedKeyCount + 1)]
    public CryptoKey Sender { get; protected set; }

    [Key(Proof.ReservedKeyCount + 2)]
    public CryptoKey Recipient { get; protected set; }

    public TransferProof2(Credit credit, CryptoKey sender, CryptoKey recipient)
    {
        this.Credit = credit;
        this.Sender = sender;
        this.Recipient = recipient;
    }

    public override SignaturePublicKey GetSignatureKey()
    {
        this.Sender.TryGetPublicKey(out var publicKey);
        return publicKey;
    }

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.Credit;
        return true;
    }

    public override bool PrepareForSigning(ref SignaturePublicKey publicKey, int validitySeconds)
    {
        if (!this.GetSignatureKey().Equals(ref publicKey))
        {
            return false;
        }

        return base.PrepareForSigning(ref publicKey, validitySeconds);
    }
}

/*[TinyhandObject]
public partial class TransferProof : ProofWithSigner
{
    public TransferProof(Value value)
        : base(value)
    {
    }

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner;

    [Key(MergeableProof.ReservedKeyCount)]
    public Point Point { get; private set; }

    [Key(MergeableProof.ReservedKeyCount + 1)]
    public SignaturePublicKey RecipientKey { get; protected set; }
}*/
