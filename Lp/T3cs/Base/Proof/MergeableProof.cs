// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public enum MergeableProofKey : int
{
    TransferProof,
}

// [TinyhandUnion((int)MergeableProofKey.TransferProof, typeof(TransferProof))]
[TinyhandObject(ReservedKeyCount = MergeableProof.ReservedKeyCount)]
// [ValueLinkObject]
public abstract partial class MergeableProof : ProofWithSigner
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = ProofWithSigner.ReservedKeyCount;

    // [Link(Primary = true, Type = ChainType.Ordered, TargetMember = "SignedMics")]
    public MergeableProof(Value value)
        : base(value)
    {
    }
}
