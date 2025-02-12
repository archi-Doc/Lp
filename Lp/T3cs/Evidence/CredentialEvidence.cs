// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Tinyhand.IO;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class CredentialEvidence : Evidence
{
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
    }

    #endregion

    public override Proof Proof => this.CredentialProof;

    public SignaturePublicKey CredentialKey
        => this.CredentialProof.CredentialKey;

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
}
