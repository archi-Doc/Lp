// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class TransferProof : MergeableProof
{
    public TransferProof(Value value)
        : base(value)
    {
    }

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner;

    public override long Difference => this.Value.Point;

    [Key(MergeableProof.ReservedKeyCount)]
    public Point Point { get; private set; }

    [Key(MergeableProof.ReservedKeyCount + 1)]
    public SignaturePublicKey RecipientKey { get; protected set; }
}
