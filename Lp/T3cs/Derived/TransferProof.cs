// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class TransferProof : ProofWithPublicKey
{
    public TransferProof()
    {
    }

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public Point Point { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 1)]
    public SignaturePublicKey RecipientKey { get; protected set; }
}
