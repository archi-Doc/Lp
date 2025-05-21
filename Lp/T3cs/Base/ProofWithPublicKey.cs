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

    public override SignaturePublicKey GetSignatureKey() => this.PublicKey;

    internal void PrepareSignInternal(SeedKey seedKey, long validMics)
    {
        this.PrepareSignInternal(validMics);
        this.PublicKey = seedKey.GetSignaturePublicKey();
    }
}
