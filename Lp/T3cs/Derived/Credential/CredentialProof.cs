// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public sealed partial class CredentialProof : ProofWithSigner
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

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner | PermittedSigner.Merger | PermittedSigner.LpKey;

    public override long MaxValidMics => Mics.MicsPerDay * 1;

    public SignaturePublicKey PublicKey => this.GetSignatureKey();

    #endregion

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = nameof(PublicKey))]
    public CredentialProof(Value value, CredentialKind kind, CredentialState state)
        : base(value)
    {
        this.Value = value;
        this.Kind = kind;
        this.State = state;
    }

    public override bool Validate()
    {
        if (!base.Validate())
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

    public override string ToString(IConversionOptions? conversionOptions)
        => $"CredentialProof:{this.Kind} {this.SignedMics.MicsToDateTimeString()} {this.Value.ToString(conversionOptions)}, {this.State.ToString(conversionOptions)}";
}
