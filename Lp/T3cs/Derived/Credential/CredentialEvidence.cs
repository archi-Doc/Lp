// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public partial class CredentialEvidence : Evidence
{
    #region GoshujinClass

    [TinyhandObject(External = true)]
    public partial class GoshujinClass
    {
        [IgnoreMember]
        public bool SyncAlias { get; set; }
    }

    #endregion

    #region Integrality

    public class Integrality : Integrality<CredentialEvidence.GoshujinClass, CredentialEvidence>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 1_000,
            RemoveIfItemNotFound = false,
        };

        public override bool Validate(CredentialEvidence.GoshujinClass goshujin, CredentialEvidence newItem, CredentialEvidence? oldItem)
        {
            if (oldItem is not null &&
                oldItem.Proof.SignedMics >= newItem.Proof.SignedMics)
            {
                return false;
            }

            if (!newItem.ValidateAndVerify())
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
    public CredentialProof Proof { get; protected set; }

    public override Proof BaseProof => this.Proof;

    public SignaturePublicKey CredentialKey => this.Proof.Value.Owner;

    #endregion

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "CredentialKey")]
    public CredentialEvidence(CredentialProof credentialProof)
    {
        this.Proof = credentialProof;
    }

    protected void CredentialKeyLinkAdded()
    {
        if (this.Goshujin?.SyncAlias == true)
        {
            Alias.Instance.TryAdd(this.Proof.State.Name, this.CredentialKey);
        }
    }

    protected void CredentialKeyLinkRemoved()
    {
        if (this.Goshujin?.SyncAlias == true)
        {
            Alias.Instance.Remove(this.CredentialKey);
        }
    }
}
