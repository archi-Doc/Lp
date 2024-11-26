// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ProofAndPublicKey : Proof
{
    [Key(0)] // Key(0) is not used in the Proof class (reserved).
    public SignaturePublicKey2 PublicKey { get; protected set; }

    public override SignaturePublicKey2 GetPublicKey()
        => this.PublicKey;

    internal void PrepareSignInternal(SeedKey seedKey, long validMics)
    {
        this.PrepareSignInternal(validMics);
        this.PublicKey = seedKey.GetSignaturePublicKey();
    }
}
