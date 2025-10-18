// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public sealed partial class CredentialProof : ProofWithPublicKey
{
    #region Integrality

    public class Integrality : Integrality<GoshujinClass, CredentialProof>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 1_000,
            RemoveIfItemNotFound = false,
        };

        public override bool Validate(GoshujinClass goshujin, CredentialProof newItem, CredentialProof? oldItem)
        {
            if (oldItem is not null &&
                oldItem.SignedMics >= newItem.SignedMics)
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

    [Key(ProofWithSigner.ReservedKeyCount + 0)]
    public CredentialKind Kind { get; private set; }

    [Key(ProofWithSigner.ReservedKeyCount + 1)]
    public CredentialState State { get; private set; }

    [Key(ProofWithSigner.ReservedKeyCount + 2)]
    public Credit UnderlyingCredit { get; private set; } = Credit.UnsafeConstructor();

    public override int MaxValiditySeconds => 3600 * 24 * 1;

    #endregion

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = nameof(PublicKey))]
    public CredentialProof(SignaturePublicKey publicKey, CredentialKind kind, CredentialState state)
        : base(publicKey)
    {
        this.Kind = kind;
        this.State = state;
    }

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        if (!this.State.IsValid)
        {
            return false;
        }

        return true;
    }

    public override string ToString() => this.ToString(default);

    /*public override string ToString(IConversionOptions? conversionOptions)
        => $"CredentialProof:{this.Kind} {this.SignedMics.MicsToDateTimeString()} {this.Value.ToString(conversionOptions)}, {this.State.ToString(conversionOptions)}";*/
}
