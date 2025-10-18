// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ProofWithPublicKey : Proof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Proof.ReservedKeyCount + 1;

    [Key(Proof.ReservedKeyCount)]
    public SignaturePublicKey PublicKey { get; protected set; }

    public ProofWithPublicKey(SignaturePublicKey publicKey)
    {
        this.PublicKey = publicKey;
    }

    public override SignaturePublicKey GetSignatureKey() => this.PublicKey;

    public override bool PrepareForSigning(ref SignaturePublicKey publicKey, int validitySeconds)
    {
        base.PrepareForSigning(ref publicKey, validitySeconds);
        this.PublicKey = publicKey;
        return true;
    }
}
