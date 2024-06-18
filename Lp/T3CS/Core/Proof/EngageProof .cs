// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

/*[TinyhandObject]
public partial class EngageProof : Proof
{
    public EngageProof()
    {
    }

    public EngageProof(ulong salt)
    {
        this.Salt = salt;
    }

    [Key(5)]
    public ulong Salt { get; protected set; }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }
        else if (this.Salt == 0)
        {
            return false;
        }

        return true;
    }

    public bool ValidateAndVerify(ulong salt)
    {
        if (this.Salt != salt)
        {
            return false;
        }

        return TinyhandHelper.ValidateAndVerify(this);
    }
}*/
