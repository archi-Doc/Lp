﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

/// <summary>
/// Represents a proof object (authentication between merger and public key).<br/>
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
public abstract partial class Proof : IEquatable<Proof>
{
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

    // /// <inheritdoc/>
    // SignaturePublicKey IVerifiable.PublicKey => this.GetSignatureKey();

    // [Key(0)] -> ProofAndPublicKey, ProofAndCredit, ProofAndValue
    // public SignaturePublicKey PublicKey { get; }

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    [Key(1, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[] Signature { get; protected set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the signed time in microseconds.
    /// </summary>
    [Key(2)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public long SignedMics { get; protected set; }

    /// <summary>
    /// Gets or sets the expiration time in microseconds.
    /// </summary>
    [Key(3)]
    public long ExpirationMics { get; protected set; }

    /// <summary>
    /// Gets the maximum valid microseconds.
    /// </summary>
    public virtual long MaxValidMics => Mics.MicsPerDay * 1;

    #endregion

    /// <summary>
    /// Gets the public signature key associated with the proof.
    /// </summary>
    /// <returns>The public key.</returns>
    public abstract SignaturePublicKey GetSignatureKey();

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
        if (this.SignedMics == 0 || this.ExpirationMics == 0)
        {
            return false;
        }

        var period = this.ExpirationMics - this.SignedMics;
        if (period < 0 || period > this.MaxValidMics)
        {
            return false;
        }

        if (!MicsRange.IsWithinMargin(Mics.FastCorrected, this.SignedMics, this.ExpirationMics))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified <see cref="Proof"/> is equal to the current <see cref="Proof"/>.
    /// </summary>
    /// <param name="other">The proof to compare with the current proof.</param>
    /// <returns><c>true</c> if the specified proof is equal to the current proof; otherwise, <c>false</c>.</returns>
    public bool Equals(Proof? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.SignedMics == other.SignedMics &&
            this.ExpirationMics == other.ExpirationMics &&
            this.Signature.SequenceEqual(other.Signature) &&
            this.GetSignatureKey().Equals(other.GetSignatureKey());
    }

    /// <summary>
    /// Prepares the proof for signing by setting the verification and expiration times.
    /// </summary>
    /// <param name="validMics">The valid microseconds.</param>
    internal void PrepareSignInternal(long validMics)
    {
        this.SignedMics = Mics.GetCorrected();
        var mics = this.SignedMics + Math.Max(validMics, this.MaxValidMics);
        this.ExpirationMics = mics / TruncateExpirationMics * TruncateExpirationMics;
    }

    /// <summary>
    /// Sets the signature for the proof.
    /// </summary>
    /// <param name="sign">The signature.</param>
    internal void SetSignInternal(byte[] sign)
    {
        this.Signature = sign;
    }
}
