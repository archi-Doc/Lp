// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class ExchangeProof : LinkableProof
{
    public ExchangeProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value, linkerPublicKey)
    {
    }

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner;

    [Key(LinkableProof.ReservedKeyCount)]
    public Point Point { get; private set; }

    [Key(LinkableProof.ReservedKeyCount + 1)]
    public SignaturePublicKey RecipientKey { get; protected set; }
}
