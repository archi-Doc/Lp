// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true)]
public partial class CertificateProof : ProofWithPublicKey
{
    #region FieldAndProperty

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public MergedProof MergedProof { get; protected set; } = MergedProof.UnsafeConstructor();

    [Key(ProofWithPublicKey.ReservedKeyCount + 1)]
    public NetNode NetNode { get; protected set; } = new();

    #endregion

    public CertificateProof(SignaturePublicKey publicKey)
        : base(publicKey)
    {
    }

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        if (!this.NetNode.Validate())
        {
            return false;
        }

        return true;
    }
}
