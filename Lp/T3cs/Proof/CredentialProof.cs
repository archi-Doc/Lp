// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public sealed partial class CredentialProof : Proof
{// Credentials = CredentialProof.Goshujin
    public const long LpExpirationMics = Mics.MicsPerDay * 10;

    #region Integrality

    public class Integrality : Integrality<CredentialProof.GoshujinClass, CredentialProof>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 1_000,
            RemoveIfItemNotFound = false,
        };

        public override bool Validate(CredentialProof.GoshujinClass goshujin, CredentialProof newItem, CredentialProof? oldItem)
        {
            if (!newItem.TryGetValueProof(out var valueProof))
            {
                return false;
            }

            if (oldItem is not null &&
                oldItem.TryGetValueProof(out var valueProof2) &&
                valueProof2.VerificationMics >= valueProof.VerificationMics)
            {
                return false;
            }

            if (!newItem.ValidateAndVerify())
            {
                return false;
            }

            var publicKey = valueProof.GetPublicKey();
            if (publicKey.Equals(LpConstants.LpPublicKey))
            {// Lp key
            }
            else if (goshujin.OriginatorChain.FindFirst(publicKey) is null)
            {// Not found
                return false;
            }

            return true;
        }
    }

    #endregion

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Originator")]
    public CredentialProof()
    {
        this.NetNode = new();
    }

    private CredentialProof(NetNode netNode)
    {
        this.NetNode = netNode;
    }

    public static CredentialProof Create(Evidence valueProofEvidence, NetNode netNode)
    {
        var credentialProof = new CredentialProof(netNode);
        credentialProof.ValueProofEvidence = valueProofEvidence;
        return credentialProof;
    }

    #region FieldAndProperty

    [Key(Proof.ReservedKeyCount)]
    public Evidence ValueProofEvidence { get; private set; } = new();

    [Key(Proof.ReservedKeyCount + 1)]
    public NetNode NetNode { get; private set; }

    public SignaturePublicKey2 Originator => this.GetPublicKey();

    #endregion

    public bool TryGetValueProof([MaybeNullWhen(false)] out ValueProof valueProof)
    {
        valueProof = this.ValueProofEvidence.Proof as ValueProof;
        return valueProof != null;
    }

    public override SignaturePublicKey2 GetPublicKey()
        => this.TryGetValueProof(out var valueProof) ? valueProof.GetPublicKey() : default;

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        if (this.TryGetValueProof(out var valueProof))
        {
            return valueProof.TryGetCredit(out credit);
        }
        else
        {
            credit = default;
            return false;
        }
    }

    public override bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        if (this.TryGetValueProof(out var valueProof))
        {
            return valueProof.TryGetValue(out value);
        }
        else
        {
            value = default;
            return false;
        }
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }
}
