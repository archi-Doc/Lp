// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class EvolProof : ContractableProofWithValue
{
    public EvolProof(SignaturePublicKey linkerPublicKey, Value value, Value targetValue, Identity? targetIdentity)
        : base(linkerPublicKey, value)
    {
        this.TargetValue = targetValue;
        this.TargetIdentity = targetIdentity;
    }

    [Key(ContractableProofWithValue.ReservedKeyCount + 0)]
    public Value TargetValue { get; protected set; } = Value.UnsafeConstructor();

    [Key(ContractableProofWithValue.ReservedKeyCount + 1)]
    public Identity? TargetIdentity { get; protected set; }
}
