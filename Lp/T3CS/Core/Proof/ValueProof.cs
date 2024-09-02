﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class ValueProof : Proof, IEquatable<ValueProof>
{
    #region FieldAndProperty

    public override SignaturePublicKey GetPublicKey()
        => this.Value.Owner;

    public Value Value { get; private set; } = default!;

    #endregion

    public ValueProof()
    {
    }

    public ValueProof(Value value)
    {
        this.Value = value;
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
        return LpHelper.ValidateAndVerify(this);
    }

    public bool Equals(ValueProof? other)
    {
        throw new NotImplementedException();
    }

    /*public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), this.Value.GetHashCode());*/
}
