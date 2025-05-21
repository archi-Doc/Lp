// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public enum LinkableProofKey : int
{
    ExchangeProof,
}

[TinyhandUnion((int)LinkableProofKey.ExchangeProof, typeof(ExchangeProof))]
[TinyhandObject(ReservedKeyCount = LinkageProof.ReservedKeyCount)]
public abstract partial class LinkableProof : LinkageProof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = LinkageProof.ReservedKeyCount;

    public LinkableProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value, linkerPublicKey)
    {
    }
}
