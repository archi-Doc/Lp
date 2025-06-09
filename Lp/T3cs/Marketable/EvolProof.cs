// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class EvolProof : ContractableProofWithSigner
{
    public EvolProof(SignaturePublicKey linkerPublicKey, Value value, Value targetValue, Identity? targetIdentity)
        : base(linkerPublicKey, value)
    {
        this.TargetValue = targetValue;
        this.TargetIdentity = targetIdentity;
    }

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner;

    [Key(ContractableProofWithSigner.ReservedKeyCount + 0)]
    public Value TargetValue { get; protected set; } = Value.UnsafeConstructor();

    [Key(ContractableProofWithSigner.ReservedKeyCount + 1)]
    public Identity? TargetIdentity { get; protected set; }
}
