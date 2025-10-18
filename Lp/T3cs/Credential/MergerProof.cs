// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

/// <summary>
/// A class that proves the validity of a merger.
/// The proof key must be either the merger itself or LpKey.
/// LpKey can either enable or disable the merger, whereas a merger key can only disable it (purge).
/// </summary>
[TinyhandObject]
[ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public sealed partial class MergerProof : Proof
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
    public SignaturePublicKey MergerKey { get; private set; }

    [Key(ReservedKeyCount + 1)]
    public bool UseLpKey { get; private set; }

    [Key(ReservedKeyCount + 2)]
    public bool Validity { get; private set; }

    public override SignaturePublicKey GetSignatureKey()
        => this.UseLpKey ? LpConstants.LpPublicKey : this.MergerKey;

    public override int MaxValiditySeconds => 3600 * 24 * 1;

    #endregion

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = nameof(MergerKey))]
    public MergerProof(SignaturePublicKey mergerKey)
    {// Purge
        this.MergerKey = mergerKey;
        this.UseLpKey = false;
        this.Validity = false;
    }

    public MergerProof(SignaturePublicKey mergerKey, bool validity)
    {
        this.MergerKey = mergerKey;
        this.UseLpKey = true;
        this.Validity = validity;
    }

    public override bool PrepareForSigning(ref SignaturePublicKey publicKey, int validitySeconds)
    {
        if (this.UseLpKey)
        {
            if (!LpConstants.LpPublicKey.Equals(ref publicKey))
            {
                return false;
            }
        }
        else
        {
            if (!this.MergerKey.Equals(ref publicKey))
            {
                return false;
            }
        }

        return base.PrepareForSigning(ref publicKey, validitySeconds);
    }

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        if (!this.UseLpKey && this.Validity)
        {// Non Lp key & valid
            return false;
        }

        return true;
    }

    public override string ToString() => this.ToString(default);
}
