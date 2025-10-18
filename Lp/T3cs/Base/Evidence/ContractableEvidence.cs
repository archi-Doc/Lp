// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject(ReservedKeyCount = ContractableEvidence.ReservedKeyCount, AddAlternateKey = true)]
public sealed partial class ContractableEvidence : Evidence
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Evidence.ReservedKeyCount + 6;

    public static readonly ObjectPool<ContractableEvidence> Pool = new(() => new());

    #region FieldAndProperty

    [Key(Evidence.ReservedKeyCount + 0, Level = TinyhandWriter.DefaultLevel)]
    public bool IsPrimary { get; set; }

    [Key(Evidence.ReservedKeyCount + 1)]
    public Contract Contract1 { get; private set; }

    [Key(Evidence.ReservedKeyCount + 2)]
    public Contract Contract2 { get; private set; }

    [Key(Evidence.ReservedKeyCount + 3)]
    public long LinkedMicsId { get; private set; }

    [Key(Evidence.ReservedKeyCount + 4)]
    public long ExpirationMics { get; private set; }

    public override Proof BaseProof
    {
        get
        {
            if (this.IsPrimary)
            {
                if (this.Contract1.IsProof)
                {
                    return this.Contract1.Proof;
                }
                else
                {
                    return this.Contract2.Proof;
                }
            }
            else
            {
                if (this.Contract2.IsProof)
                {
                    return this.Contract2.Proof;
                }
                else
                {
                    return this.Contract1.Proof;
                }
            }
        }
    }

    public ContractableProof Proof1 => (ContractableProof)this.Contract1.Proof;

    public ContractableProof Proof2 => (ContractableProof)this.Contract2.Proof;

    #endregion

    public ContractableEvidence(bool isPrimary, Contract contract1, Contract contract2, long linkedMicsId, long expirationMics)
    {
        this.IsPrimary = isPrimary;
        this.Contract1 = contract1;
        this.Contract2 = contract2;
        this.LinkedMicsId = linkedMicsId;
        this.ExpirationMics = expirationMics;
    }

    public ContractableEvidence(bool isPrimary, ContractableProof proof1, ContractableProof proof2, long linkedMicsId, long expirationMics)
        : this(isPrimary, new Contract(proof1), new Contract(proof2), linkedMicsId, expirationMics)
    {
    }

    private ContractableEvidence()
    {
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

        this.Contract1 = linkage.Contract1;
        this.Contract2 = linkage.Contract2;
        this.LinkedMicsId = linkage.LinkedMics;
        this.ExpirationMics = linkage.ExpirationMics;
    }
}
