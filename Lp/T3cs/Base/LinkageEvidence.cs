// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject]
// [ValueLinkObject(Integrality = false, Isolation = IsolationLevel.None)]
public sealed partial class LinkageEvidence : Evidence
{
    #region FieldAndProperty

    [Key(Evidence.ReservedKeyCount + 1)]
    // [Link(Primary = true, Unique = true, Type = ChainType.Ordered, AddValue = false)]
    public long LinkedMicsId { get; private set; }

    [Key(Evidence.ReservedKeyCount + 2)]
    public Proof LinkageProof1 { get; private set; }

    [Key(Evidence.ReservedKeyCount + 3)]
    public Proof LinkageProof2 { get; private set; }

    public override Proof BaseProof => this.LinkageProof1;

    #endregion

    public LinkageEvidence(long linkedMicsId, Proof linkageProof, Proof linkageProof2)
    {
        this.LinkedMicsId = linkedMicsId;
        this.LinkageProof1 = linkageProof;
        this.LinkageProof2 = linkageProof2;
    }

    public (Proof? Proof, int MergerIndex) GetMergerIndex(ref SignaturePublicKey publicKey)
    {
        if (this.LinkageProof1.TryGetCredit(out var credit))
        {
            var mergerIndex = credit.GetMergerIndex(ref publicKey);
            if (mergerIndex >= 0)
            {
                return (this.LinkageProof1, mergerIndex);
            }
        }

        if (this.LinkageProof2.TryGetCredit(out credit))
        {
            var mergerIndex = credit.GetMergerIndex(ref publicKey);
            if (mergerIndex >= 0)
            {
                return (this.LinkageProof2, mergerIndex);
            }
        }

        return default;
    }

    internal void FromLinkage(Linkage linkage, bool first)
    {
        this.LinkedMicsId = linkage.LinkedMics;
        this.LinkageProof1 = linkage.BaseProof1;
        this.LinkageProof2 = linkage.BaseProof2;
        if (first)
        {
            this.MergerSignature0 = linkage.MergerSignature10;
            this.MergerSignature1 = linkage.MergerSignature11;
            this.MergerSignature2 = linkage.MergerSignature12;
        }
        else
        {
            this.MergerSignature0 = linkage.MergerSignature20;
            this.MergerSignature1 = linkage.MergerSignature21;
            this.MergerSignature2 = linkage.MergerSignature22;
        }
    }
}
