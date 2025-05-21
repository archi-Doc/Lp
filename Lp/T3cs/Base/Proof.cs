// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

public enum ProofKey : int
{
    CredentialProof,
    LinkProof,
    ValueProof,
    CreateCreditProof,
    EvolProof,
    TransferProof,
    DimensionProof,
    IdentificationProof,

    TestLinkageProof,
    TemplateProof,
}

/// <summary>
/// Represents the base class of Proof.<br/>
/// This class holds an authentication key and its proof content.
/// </summary>
[TinyhandUnion((int)ProofKey.CredentialProof, typeof(CredentialProof))]
[TinyhandUnion((int)ProofKey.LinkProof, typeof(LinkProof))]
[TinyhandUnion((int)ProofKey.ValueProof, typeof(ValueProof))]
[TinyhandUnion((int)ProofKey.CreateCreditProof, typeof(CreateCreditProof))]
[TinyhandUnion((int)ProofKey.EvolProof, typeof(EvolProof))]
[TinyhandUnion((int)ProofKey.TransferProof, typeof(TransferProof))]
[TinyhandUnion((int)ProofKey.DimensionProof, typeof(DimensionProof))]
[TinyhandUnion((int)ProofKey.IdentificationProof, typeof(IdentificationProof))]
[TinyhandUnion((int)ProofKey.TestLinkageProof, typeof(TestLinkageProof))]
[TinyhandUnion((int)ProofKey.TemplateProof, typeof(TemplateProof))]
[TinyhandObject(ReservedKeyCount = Proof.ReservedKeyCount)]
public abstract partial class Proof : IEquatable<Proof>, ISignable
{
    /// <summary>
    /// The number of microseconds by which the expiration time is truncated.<br/>
    /// If the valid mics is less than or equal to this value, it will not be truncated.
    /// </summary>
    public const long TruncateExpirationMics = Mics.MicsPerDay;

    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="Proof"/> class.
    /// </summary>
    protected Proof()
    {
    }

    #region FieldAndProperty

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    [Key(0, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[] Signature { get; protected set; } = [];

    /// <summary>
    /// Gets or sets the signed time in microseconds.
    /// </summary>
    [Key(1)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public long SignedMics { get; protected set; }

    /// <summary>
    /// Gets or sets the expiration time in microseconds.
    /// </summary>
    [Key(2)]
    public long ExpirationMics { get; protected set; }

    /// <summary>
    /// Gets the maximum valid microseconds.
    /// </summary>
    public virtual long MaxValidMics => LpConstants.DefaultProofMaxValidMics;

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
    /// Tries to get the linker public key associated with the proof.
    /// </summary>
    /// <param name="linkerPublicKey"> When this method returns, contains the linker public key if available; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the linker public key is available; otherwise, <c>false</c>.</returns>
    public virtual bool TryGetLinkerPublicKey([MaybeNullWhen(false)] out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = default;
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

    public override string ToString() => this.ToString(default);

    public virtual string ToString(IConversionOptions? conversionOptions)
        => $"Proof:";

    bool ISignable.SetSignature(int signer, byte[] signature)
    {
        if (signer != 0 ||
            signature.Length != CryptoSign.SignatureSize)
        {
            return false;
        }

        this.Signature = signature;
        return true;
    }

    /// <summary>
    /// Prepares the proof for signing by setting the verification and expiration times.
    /// </summary>
    /// <param name="validMics">The valid microseconds.</param>
    internal void PrepareSignInternal(long validMics)
    {
        this.SignedMics = Mics.GetCorrected();
        validMics = Math.Max(validMics, this.MaxValidMics);
        var mics = this.SignedMics + validMics;
        if (validMics > TruncateExpirationMics)
        {
            this.ExpirationMics = mics / TruncateExpirationMics * TruncateExpirationMics;
        }
        else
        {
            this.ExpirationMics = mics;
        }
    }
}
