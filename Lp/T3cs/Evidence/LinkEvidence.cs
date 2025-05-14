// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;
using Lp.Services;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
// [ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public partial class LinkEvidence : Evidence
{
    #region Integrality

    /*public class Integrality : Integrality<CredentialEvidence.GoshujinClass, CredentialEvidence>
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

        public override int Trim(GoshujinClass goshujin, int integratedCount)
        {
            return base.Trim(goshujin, integratedCount);
        }
    }*/

    #endregion

    // public override Proof Proof => this.CredentialProof;

    // public SignaturePublicKey CredentialKey => this.CredentialProof.GetSignatureKey();

    [Key(Evidence.ReservedKeyCount)]
    public LinkProof LinkProof { get; private set; }

    public override Proof Proof => this.LinkProof;

    public LinkEvidence()
    {
    }

    public LinkEvidence(LinkProof linkProof)
    {
        this.LinkProof = linkProof;
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
}
