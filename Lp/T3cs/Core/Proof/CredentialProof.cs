// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CredentialProof : Proof
{
    public CredentialProof()
    {
    }

    #region FieldAndProperty

    [Key(5)]
    public Value Value { get; private set; } = new();

    [Key(6)]
    public NetAddress NetAddress { get; private set; }

    #endregion

    public override SignaturePublicKey GetPublicKey()
        => this.Value.Owner;

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
}
