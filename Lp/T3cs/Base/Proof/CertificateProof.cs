// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true)]
public partial class CertificateProof : Proof
{
    #region FieldAndProperty

    [Key(Proof.ReservedKeyCount)]
    public MergedProof MergedProof { get; protected set; } // = MergedProof.UnsafeConstructor();

    [Key(Proof.ReservedKeyCount + 1)]
    public NetNode NetNode { get; protected set; } // = new();

    #endregion

    public CertificateProof(MergedProof mergedProof, NetNode netNode)
    {
        this.MergedProof = mergedProof;
        this.NetNode = netNode;
    }

    public override SignaturePublicKey GetSignatureKey() => this.MergedProof.Value.Owner;

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
