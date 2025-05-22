// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject(ReservedKeyCount = LinkableEvidence.ReservedKeyCount)]
// [ValueLinkObject(Integrality = false, Isolation = IsolationLevel.None)]
public sealed partial class LinkableEvidence : Evidence
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Evidence.ReservedKeyCount + 4;

    public static readonly ObjectPool<LinkableEvidence> Pool = new(() => LinkableEvidence.UnsafeConstructor());

    #region FieldAndProperty

    [Key(Evidence.ReservedKeyCount + 0, Level = TinyhandWriter.DefaultLevel)]
    public bool IsPrimary { get; set; }

    [Key(Evidence.ReservedKeyCount + 1)]
    // [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public long LinkedMicsId { get; private set; }

    [Key(Evidence.ReservedKeyCount + 2)]
    public Proof BaseProof1 { get; private set; }

    [Key(Evidence.ReservedKeyCount + 3)]
    public Proof BaseProof2 { get; private set; }

    public override Proof BaseProof => this.IsPrimary ? this.BaseProof1 : this.BaseProof2;

    public LinkableProof LinkageProof1 => (LinkableProof)this.BaseProof1;

    public LinkableProof LinkageProof2 => (LinkableProof)this.BaseProof2;

    #endregion

    public LinkableEvidence(bool isPrimary, long linkedMicsId, LinkableProof linkageProof, LinkableProof linkageProof2)
    {
        this.IsPrimary = isPrimary;
        this.LinkedMicsId = linkedMicsId;
        this.BaseProof1 = linkageProof;
        this.BaseProof2 = linkageProof2;
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
        this.BaseProof1 = linkage.BaseProof1;
        this.BaseProof2 = linkage.BaseProof2;
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
