// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public enum MarketableProofKey : int
{
    ExchangeProof,
}

[TinyhandUnion((int)MarketableProofKey.ExchangeProof, typeof(ExchangeProof))]
[TinyhandObject(ReservedKeyCount = ContractableProof.ReservedKeyCount)]
public abstract partial class MarketableProof : ContractableProof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = ContractableProof.ReservedKeyCount;

    public MarketableProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value, linkerPublicKey)
    {
    }
}
