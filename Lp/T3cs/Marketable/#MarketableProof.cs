// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public enum MarketableProofKey : int
{
    ExchangeProof,
}

[TinyhandUnion((int)MarketableProofKey.ExchangeProof, typeof(ExchangeProof))]
[TinyhandObject(ReservedKeyCount = LinkableProof.ReservedKeyCount)]
public abstract partial class MarketableProof : LinkableProof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = LinkableProof.ReservedKeyCount;

    public MarketableProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value, linkerPublicKey)
    {
    }
}
