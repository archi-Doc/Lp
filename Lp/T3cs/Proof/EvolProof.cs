// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial class EvolProof : ProofWithPublicKey
{
    public EvolProof()
    {
    }

    [Key(Proof.ReservedKeyCount)]
    public Point Point { get; private set; }
}
