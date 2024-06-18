// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

[TinyhandObject]
public partial class TemplateProof : Proof
{// Remember to add the TinyhandUnion attribute to the Proof class.
    public TemplateProof()
    {
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }

    public bool ValidateAndVerify()
    {
        return TinyhandHelper.ValidateAndVerify(this);
    }
}
