// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CryptoTransferProof : ProofWithPublicKey
{
    private const int OriginalKeyLevel = 1_000;

    public CryptoTransferProof()
    {
    }

    [Key(Proof.ReservedKeyCount)]
    public Point Point { get; private set; }

    public SignaturePublicKey SenderKey => this.PublicKey;

    // Encryption, (PublicKey), Encrypted

    [Key(Proof.ReservedKeyCount + 1)]
    public uint SenderEncryption { get; private set; }

    [Key(Proof.ReservedKeyCount + 2)]
    public byte[]? SenderEncrypted { get; private set; }

    [Key(Proof.ReservedKeyCount + 3, Level = OriginalKeyLevel)]
    public SignaturePublicKey SenderOriginalKey { get; private set; }

    [Key(Proof.ReservedKeyCount + 4)]
    public uint RecipientEncryption { get; private set; }

    [Key(Proof.ReservedKeyCount + 5)]
    public SignaturePublicKey RecipientKey { get; private set; }

    [Key(Proof.ReservedKeyCount + 6)]
    public byte[]? RecipientEncrypted { get; private set; }

    [Key(Proof.ReservedKeyCount + 7, Level = OriginalKeyLevel)]
    public SignaturePublicKey RecipientOriginalKey { get; private set; }

    public bool TryGetSenderKey(out SignaturePublicKey senderKey)
    {
        senderKey = default;
        return false;
    }
}
