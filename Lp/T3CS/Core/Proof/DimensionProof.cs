// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial class IdentificationProof : Proof
{
    public IdentificationProof()
    {
    }

    [Key(5)]
    public string Name { get; private set; } = string.Empty;

    public bool ValidateAndVerify()
    {
        return LP.TinyhandHelper.ValidateAndVerify(this);
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }
}
