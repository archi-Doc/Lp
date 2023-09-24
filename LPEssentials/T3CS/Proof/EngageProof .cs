// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

[TinyhandObject]
public partial class EngageProof : Proof
{
    public EngageProof()
    {
    }

    public EngageProof(long salt)
    {
        this.Salt = salt;
    }

    [Key(5)]
    public long Salt { get; protected set; }

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
}
