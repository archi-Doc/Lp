// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class TemplateProof : ProofWithPublicKey
{// Don't forget to add the TinyhandUnion attribute to the Proof class.
    public TemplateProof(SignaturePublicKey publicKey)
        : base(publicKey)
    {
    }

    /* public override int MaxValiditySeconds => Seconds.FromDays(1);

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        return true;
    }*/
}
