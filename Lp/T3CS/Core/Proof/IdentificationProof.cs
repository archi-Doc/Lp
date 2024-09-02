// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial class IdentificationProof : ProofAndPublicKey
{
    public IdentificationProof()
    {
    }

    [Key(Proof.ReservedKeyCount)]
    public string Name { get; private set; } = string.Empty;

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }
}
