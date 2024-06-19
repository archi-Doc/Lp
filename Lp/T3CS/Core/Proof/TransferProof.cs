// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class TransferProof : Proof
{
    public TransferProof()
    {
    }

    [Key(5)]
    public Point Point { get; private set; }

    [Key(6)]
    public SignaturePublicKey RecipientKey { get; protected set; }

    public bool ValidateAndVerify()
    {
        return LpHelper.ValidateAndVerify(this);
    }
}
