// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
// [ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public sealed partial class CredentialProof : ProofWithSigner
{// Credentials = CredentialProof.Goshujin
    #region Integrality

    /*public class Integrality : Integrality<CredentialProof.GoshujinClass, CredentialProof>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 1_000,
            RemoveIfItemNotFound = false,
        };

        public override bool Validate(CredentialProof.GoshujinClass goshujin, CredentialProof newItem, CredentialProof? oldItem)
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
    }*/

    #endregion

    // [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Originator")]
    public CredentialProof(Value value, CredentialKind kind, CredentialState state)
    {
        this.Value = value;
        this.Kind = kind;
        this.State = state;
    }

    #region FieldAndProperty

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner | PermittedSigner.Merger | PermittedSigner.LpKey;

    [Key(ProofWithSigner.ReservedKeyCount + 0)]
    public CredentialKind Kind { get; private set; }

    [Key(ProofWithSigner.ReservedKeyCount + 1)]
    public CredentialState State { get; private set; }

    public SignaturePublicKey Originator => this.GetSignatureKey();

    public override long MaxValidMics => Mics.MicsPerDay * 1;

    #endregion

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
