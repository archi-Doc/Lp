// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class MergeableEvidence : Evidence
{
    #region GoshujinClass

    [TinyhandObject(External = true)]
    public partial class GoshujinClass
    {
    }

    #endregion

    #region Integrality

    public class Integrality : Integrality<MergeableEvidence.GoshujinClass, MergeableEvidence>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 1_000,
            RemoveIfItemNotFound = false,
        };

        public override bool Validate(MergeableEvidence.GoshujinClass goshujin, MergeableEvidence newItem, MergeableEvidence? oldItem)
        {
            if (oldItem is not null &&
                oldItem.Proof.SignedMics >= newItem.Proof.SignedMics)
            {
                return false;
            }

            if (!newItem.ValidateAndVerify(default))
            {
                return false;
            }

            return true;
        }

        /*public override int Trim(GoshujinClass goshujin, int integratedCount)
        {
            return base.Trim(goshujin, integratedCount);
        }*/
    }

    #endregion

    #region FieldAndProperty

    [Key(Evidence.ReservedKeyCount)]
    public MergeableProof Proof { get; protected set; }

    public override Proof BaseProof => this.Proof;

    public long SignedMics => this.Proof.SignedMics;

    #endregion

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "SignedMics")]
    public MergeableEvidence(MergeableProof proof)
    {
        this.Proof = proof;
    }
}
