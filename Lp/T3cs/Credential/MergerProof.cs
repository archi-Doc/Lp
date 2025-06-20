// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public sealed partial class MergerProof : ProofWithPublicKey
{
    #region Integrality

    public class Integrality : Integrality<GoshujinClass, MergerProof>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 1_000,
            RemoveIfItemNotFound = false,
        };

        public override bool Validate(GoshujinClass goshujin, MergerProof newItem, MergerProof? oldItem)
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

    [Key(ReservedKeyCount + 0)]
    public bool UseLpKey { get; private set; }

    [Key(ReservedKeyCount + 1)]
    public bool Validity { get; private set; }

    public override SignaturePublicKey GetSignatureKey()
        => this.UseLpKey ? LpConstants.LpPublicKey : this.PublicKey;

    public override long MaxValidMics => Mics.MicsPerDay * 1;

    #endregion

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = nameof(PublicKey))]
    public MergerProof(SignaturePublicKey publicKey)
        : base(publicKey)
    {
    }

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        if (!this.UseLpKey && this.Validity)
        {
            return false;
        }

        return true;
    }

    public override string ToString() => this.ToString(default);
}
