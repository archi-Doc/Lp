// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;
using Lp.Services;
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

        public void Validate()
        {
            using (this.LockObject.EnterScope())
            {
                TemporaryList<CredentialEvidence> toDelete = default;
                foreach (var evidence in this)
                {
                    if (!evidence.Validate())
                    {
                        toDelete.Add(evidence);
                    }
                }

                foreach (var evidence in toDelete)
                {
                    this.Remove(evidence);
                }
            }
        }

        public bool TryAdd(CredentialEvidence evidence)
        {
            if (evidence.ValidateAndVerify() != true)
            {
                return false;
            }

            using (this.lockObject.EnterScope())
            {
                return ((IIntegralityGoshujin)this).IntegrateObject(Integrality.Default, evidence) == IntegralityResult.Success;
            }
        }
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
                oldItem.ProofMics >= newItem.ProofMics)
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

    public override Proof Proof => this.CredentialProof;

    public SignaturePublicKey CredentialKey
        => this.CredentialProof.GetSignatureKey();

    [Key(0)]
    public CredentialProof CredentialProof { get; protected set; } = default!;

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "CredentialKey")]
    public CredentialEvidence()
    {
    }

    public CredentialEvidence(CredentialProof credentialProof)
    {
        this.CredentialProof = credentialProof;
    }

    public static bool TryCreate(CredentialProof proof, SeedKey seedKey, [MaybeNullWhen(false)] out CredentialEvidence evidence)
    {
        var obj = new CredentialEvidence(proof);
        if (!obj.TrySign(seedKey, 0))
        {
            evidence = default;
            return false;
        }

        evidence = obj;
        return true;
    }

    protected void CredentialKeyLinkAdded()
    {
        if (this.Goshujin?.SyncAlias == true)
        {
            Alias.Instance.TryAdd(this.CredentialProof.State.Name, this.CredentialKey);
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
