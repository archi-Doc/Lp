// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ProofWithPublicKey : Proof
{
    [Key(0)] // Key(0) is not used in the Proof class (reserved).
    public SignaturePublicKey PublicKey { get; protected set; }

    public override SignaturePublicKey GetSignatureKey() => this.PublicKey;

    internal void PrepareSignInternal(SeedKey seedKey, long validMics)
    {
        this.PrepareSignInternal(validMics);
        this.PublicKey = seedKey.GetSignaturePublicKey();
    }
}
