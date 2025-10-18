// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ContractableProofWithPublicKey : ContractableProof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = ContractableProof.ReservedKeyCount + 1;

    [Key(ContractableProof.ReservedKeyCount)]
    public SignaturePublicKey PublicKey { get; protected set; }

    public ContractableProofWithPublicKey(SignaturePublicKey linkerPublicKey, SignaturePublicKey publicKey)
        : base(linkerPublicKey)
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
