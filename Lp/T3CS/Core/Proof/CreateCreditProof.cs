// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial class CreateCreditProof : Proof
{
    public CreateCreditProof()
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
        return Lp.TinyhandHelper.ValidateAndVerify(this);
    }
}
