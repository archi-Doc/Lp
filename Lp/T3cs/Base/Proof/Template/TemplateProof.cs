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

    // public override long MaxValidMics => Mics.MicsPerDay * 1;

    /*public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }*/
}
