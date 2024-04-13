// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

[TinyhandObject]
public partial class EvolProof : Proof
{
    public EvolProof()
    {
    }

    [Key(5)]
    public Point Point { get; private set; }

    public bool ValidateAndVerify()
    {
        return TinyhandHelper.ValidateAndVerify(this);
    }
}
