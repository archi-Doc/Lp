// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents a proof object.
/// </summary>
[TinyhandUnion(0, typeof(ValueProof))]
[TinyhandUnion(1, typeof(CreateCreditProof))]
[TinyhandUnion(2, typeof(EvolProof))]
[TinyhandUnion(3, typeof(TransferProof))]
[TinyhandUnion(4, typeof(DimensionProof))]
[TinyhandUnion(5, typeof(IdentificationProof))]
[TinyhandUnion(6, typeof(CredentialProof))]
[TinyhandObject(ReservedKeyCount = Proof.ReservedKeyCount)]
// [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public abstract partial class Proof : IVerifiable, IEquatable<Proof>
{
    /// <summary>
    /// The maximum expiration time in microseconds.
    /// </summary>
    public const long MaxExpirationMics = Mics.MicsPerDay * 10;

    /// <summary>
    /// The expiration time in microseconds to truncate to.
    /// </summary>
    public const long TruncateExpirationMics = Mics.MicsPerDay;

    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="Proof"/> class.
    /// </summary>
    public Proof()
    {
    }

    #region FieldAndProperty

    /// <inheritdoc/>
    SignaturePublicKey IVerifiable.PublicKey => this.GetPublicKey();

    // [Key(0)] -> ProofAndPublicKey, ProofAndCredit, ProofAndValue
    // public SignaturePublicKey PublicKey { get; }

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    [Key(1, Level = 1)]
    public byte[] Signature { get; protected set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the verification time in microseconds.
    /// </summary>
    [Key(2)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public long VerificationMics { get; protected set; }

    /// <summary>
    /// Gets or sets the expiration time in microseconds.
    /// </summary>
    [Key(3)]
    public long ExpirationMics { get; protected set; }

    #endregion

    /// <summary>
    /// Gets the public key associated with the proof.
    /// </summary>
    /// <returns>The public key.</returns>
    public abstract SignaturePublicKey GetPublicKey();

    /// <summary>
    /// Tries to get the credit associated with the proof.
    /// </summary>
    /// <param name="credit">The credit associated with the proof, if available.</param>
    /// <returns><c>true</c> if the credit is available; otherwise, <c>false</c>.</returns>
    public virtual bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = default;
        return false;
    }

    /// <summary>
    /// Tries to get the value associated with the proof.
    /// </summary>
    /// <param name="value">The value associated with the proof, if available.</param>
    /// <returns><c>true</c> if the value is available; otherwise, <c>false</c>.</returns>
    public virtual bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = default;
        return false;
    }

    /// <summary>
    /// Validates the proof.
    /// </summary>
    /// <returns><c>true</c> if the proof is valid; otherwise, <c>false</c>.</returns>
    public virtual bool Validate()
    {
        if (this.VerificationMics == 0 || this.ExpirationMics == 0)
        {
            return false;
        }

        var period = this.ExpirationMics - this.VerificationMics;
        if (period < 0 || period > MaxExpirationMics)
        {
            return false;
        }

        if (!MicsRange.IsWithinMargin(Mics.FastCorrected, this.VerificationMics, this.ExpirationMics))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool Equals(Proof? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.VerificationMics == other.VerificationMics &&
            this.ExpirationMics == other.ExpirationMics &&
            this.Signature.SequenceEqual(other.Signature) &&
            this.GetPublicKey().Equals(other.GetPublicKey());
    }

    internal void PrepareSignInternal(long validMics)
    {
        this.VerificationMics = Mics.GetCorrected();
        var mics = this.VerificationMics + (validMics > MaxExpirationMics ? MaxExpirationMics : validMics);
        this.ExpirationMics = mics / TruncateExpirationMics * TruncateExpirationMics;
    }

    internal void SetSignInternal(byte[] sign)
    {
        this.Signature = sign;
    }
}
