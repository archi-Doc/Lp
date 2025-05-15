// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class ValueProof : ProofWithValue, IEquatable<ValueProof>
{
    #region FieldAndProperty

    #endregion

    public ValueProof()
    {
    }

    public static ValueProof Create(Value value)
    {
        var proof = new ValueProof();
        proof.Value = value;
        return proof;
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }

    public bool Equals(ValueProof? other)
    {
        throw new NotImplementedException();
    }

    /*public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), this.Value.GetHashCode());*/
}
