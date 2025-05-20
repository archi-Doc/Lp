// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(ReservedKeyCount = LinkageEvidence.ReservedKeyCount)]
// [ValueLinkObject(Integrality = false, Isolation = IsolationLevel.None)]
public sealed partial class LinkageEvidence : Evidence
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Evidence.ReservedKeyCount + 4;

    public static readonly ObjectPool<LinkageEvidence> Pool = new(() => LinkageEvidence.UnsafeConstructor());

    #region FieldAndProperty

    [Key(Evidence.ReservedKeyCount + 0, Level = int.MaxValue)]
    public bool IsPrimary { get; private set; }

    [Key(Evidence.ReservedKeyCount + 1)]
    // [Link(Primary = true, Unique = true, Type = ChainType.Ordered, AddValue = false)]
    public long LinkedMicsId { get; private set; }

    [Key(Evidence.ReservedKeyCount + 2)]
    public Proof LinkageProof1 { get; private set; }

    [Key(Evidence.ReservedKeyCount + 3)]
    public Proof LinkageProof2 { get; private set; }

    public override Proof BaseProof => this.IsPrimary ? this.LinkageProof1 : this.LinkageProof2;

    #endregion

    public LinkageEvidence(bool isPrimary, long linkedMicsId, LinkageProof linkageProof, LinkageProof linkageProof2)
    {
        this.IsPrimary = isPrimary;
        this.LinkedMicsId = linkedMicsId;
        this.LinkageProof1 = linkageProof;
        this.LinkageProof2 = linkageProof2;
    }

    public (Proof? Proof, int MergerIndex) GetMergerIndex(ref SignaturePublicKey publicKey)
    {
        if (this.BaseProof.TryGetCredit(out var credit))
        {
            var mergerIndex = credit.GetMergerIndex(ref publicKey);
            if (mergerIndex >= 0)
            {
                return (this.BaseProof, mergerIndex);
            }
        }

        return default;
    }

    internal void FromLinkage(Linkage linkage, bool isPrimary)
    {
        this.LinkedMicsId = linkage.LinkedMics;
        this.LinkageProof1 = linkage.BaseProof1;
        this.LinkageProof2 = linkage.BaseProof2;
        if (isPrimary)
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
