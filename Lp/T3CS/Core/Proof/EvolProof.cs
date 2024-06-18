// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

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
        return LP.TinyhandHelper.ValidateAndVerify(this);
    }
}
