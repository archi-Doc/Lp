// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public sealed partial class CredentialProof : Proof
{// Credentials = CredentialProof.Goshujin
    public const long LpExpirationMics = Mics.MicsPerDay * 1;

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
            if (oldItem is not null &&
                oldItem.VerificationMics >= newItem.VerificationMics)
            {
                return false;
            }

            if (!newItem.ValidateAndVerify())
            {
                return false;
            }

            var publicKey = newItem.GetSignatureKey();
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
        this.Value = default!;
        this.State = default!;
    }

    public CredentialProof(SignaturePublicKey credentialKey, Value value, CredentialState state)
    {
        this.CredentialKey = credentialKey;
        this.Value = value;
        this.State = state;
    }

    #region FieldAndProperty

    [Key(Proof.ReservedKeyCount)]
    public SignaturePublicKey CredentialKey { get; private set; }

    [Key(Proof.ReservedKeyCount + 1)]
    public Value Value { get; private set; }

    [Key(Proof.ReservedKeyCount + 2)]
    public CredentialState State { get; private set; }

    public SignaturePublicKey Originator => this.GetSignatureKey();

    public override long MaxValidMics => Mics.MicsPerDay * 1;

    #endregion

    public override SignaturePublicKey GetSignatureKey()
        => this.CredentialKey;

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.Value.Credit;
        return true;
    }

    public override bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = this.Value;
        return true;
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
