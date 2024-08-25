// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class CryptoTransferProof : Proof
{
    private const int OriginalKeyLevel = 1_000;

    public CryptoTransferProof()
    {
    }

    [Key(Proof.ReservedKeyCount)]
    public Point Point { get; private set; }

    public SignaturePublicKey SenderKey => this.PublicKey;

    // CryptoSeed, EncryptionPublicKey, Validation

    [Key(Proof.ReservedKeyCount + 1)]
    public SignaturePublicKey SenderCryptoKey { get; protected set; }

    [Key(Proof.ReservedKeyCount + 2, Level = OriginalKeyLevel)]
    public SignaturePublicKey SenderOriginalKey { get; protected set; }

    [Key(Proof.ReservedKeyCount + 3)]
    public SignaturePublicKey RecipientKey { get; protected set; }

    [Key(Proof.ReservedKeyCount + 4)]
    public SignaturePublicKey RecipientCryptoKey { get; protected set; }

    [Key(Proof.ReservedKeyCount + 5, Level = OriginalKeyLevel)]
    public SignaturePublicKey RecipientOriginalKey { get; protected set; }

    public bool TryGetSenderKey(out SignaturePublicKey senderKey)
    {
        senderKey = default;
        return false;
    }

    public bool ValidateAndVerify()
    {
        return LpHelper.ValidateAndVerify(this);
    }
}
