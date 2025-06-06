// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class EvolProof : MarketableProof
{
    public EvolProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value, linkerPublicKey)
    {
    }

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner;

    // [Key(MarketableProof.ReservedKeyCount)]
    // public Point TargetPoint { get; private set; }

    [Key(MarketableProof.ReservedKeyCount + 0)]
    public Value TargetValue { get; protected set; } = Value.UnsafeConstructor();

    [Key(MarketableProof.ReservedKeyCount + 1)]
    public Identity? TargetIdentity { get; protected set; }
}
