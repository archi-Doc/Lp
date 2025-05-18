// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class ValueProof : ProofWithValue
{
    #region FieldAndProperty

    #endregion

    public ValueProof(Value value)
        : base(value)
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
}
