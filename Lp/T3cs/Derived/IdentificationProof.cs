// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class IdentificationProof : ProofWithPublicKey
{
    public IdentificationProof(SignaturePublicKey publicKey)
        : base(publicKey)
    {
    }

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public string Name { get; private set; } = string.Empty;

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        return true;
    }
}
