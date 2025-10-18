// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class CreateCreditProof : ProofWithPublicKey
{
    public CreateCreditProof(SignaturePublicKey publicKey)
        : base(publicKey)
    {
    }

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        return true;
    }
}
