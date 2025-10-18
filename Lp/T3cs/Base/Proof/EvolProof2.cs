// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

/*[TinyhandObject(AddAlternateKey = true)]
public sealed partial class EvolProof2 : Proof, IEquatable<EvolProof2>
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Proof.ReservedKeyCount + 4;

    #region FieldAndProperty

    [Key(Proof.ReservedKeyCount + 0)]
    public SignaturePublicKey LinkerPublicKey { get; private set; }

    [Key(Proof.ReservedKeyCount + 1)]
    public Value SourceValue { get; private set; }

    [Key(Proof.ReservedKeyCount + 2)]
    public Value DestinationValue { get; private set; }

    [Key(Proof.ReservedKeyCount + 3, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public Identity? DestinationIdentity { get; private set; }

    #endregion

    public EvolProof2(SignaturePublicKey linkerPublicKey, Value sourceValue, Value destinationValue, Identity? destinationIdentity)
    {
        this.LinkerPublicKey = linkerPublicKey;
        this.SourceValue = sourceValue;
        this.DestinationValue = destinationValue;
        this.DestinationIdentity = destinationIdentity;
    }

    /// <summary>
    /// Tries to get the linker public key associated with the proof.
    /// </summary>
    /// <param name="linkerPublicKey"> When this method returns, contains the linker public key if available; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the linker public key is available; otherwise, <c>false</c>.</returns>
    public bool TryGetLinkerPublicKey(out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = this.LinkerPublicKey;
        return true;
    }

    /// <summary>
    /// Determines whether the specified <see cref="Proof"/> is equal to the current <see cref="Proof"/>.
    /// </summary>
    /// <param name="other">The proof to compare with the current proof.</param>
    /// <returns><c>true</c> if the specified proof is equal to the current proof; otherwise, <c>false</c>.</returns>
    public bool Equals(EvolProof2? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.SignedMics == other.SignedMics &&
            this.ValiditySeconds == other.ValiditySeconds &&
            this.Signature.SequenceEqual(other.Signature) &&
            this.GetSignatureKey().Equals(other.GetSignatureKey()) &&
            this.LinkerPublicKey.Equals(other.LinkerPublicKey);
    }

    public override SignaturePublicKey GetSignatureKey() => this.SourceValue.Owner;

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        if (!this.SourceValue.Validate() || !this.DestinationValue.Validate())
        {
            return false;
        }

        if (this.DestinationIdentity is not null)
        {
            var identifier = this.DestinationIdentity.GetIdentifier();
            if (!this.DestinationValue.Credit.Identifier.Equals(ref identifier))
            {
                return false;
            }
        }

        return true;
    }

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.SourceValue.Credit;
        return true;
    }

    public override bool PrepareForSigning(ref SignaturePublicKey publicKey, int validitySeconds)
    {
        if (!this.SourceValue.Owner.Equals(ref publicKey))
        {
            return false;
        }

        return base.PrepareForSigning(ref publicKey, validitySeconds);
    }

    public bool ContentEquals(EvolProof2? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.SourceValue.Equals(other.SourceValue) &&
            this.DestinationValue.Equals(other.DestinationValue);
    }
}*/
