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

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public Point Point { get; private set; }

    public SignaturePublicKey SenderKey => this.PublicKey;

    // Encryption, (PublicKey), Encrypted

    [Key(ProofWithPublicKey.ReservedKeyCount + 1)]
    public uint SenderEncryption { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 2)]
    public byte[]? SenderEncrypted { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 3, Level = OriginalKeyLevel)]
    public SignaturePublicKey SenderOriginalKey { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 4)]
    public uint RecipientEncryption { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 5)]
    public SignaturePublicKey RecipientKey { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 6)]
    public byte[]? RecipientEncrypted { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 7, Level = OriginalKeyLevel)]
    public SignaturePublicKey RecipientOriginalKey { get; private set; }

    public bool TryGetSenderKey(out SignaturePublicKey senderKey)
    {
        senderKey = default;
        return false;
    }
}
