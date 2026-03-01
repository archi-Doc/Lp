// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

public enum ProofKey : int
{
    EvolProof,
    MergedProof,
    CertificateProof,
    CredentialProof,
    LinkProof,
    ValueProof,
    CreateCreditProof,
    TransferProof,
    DimensionProof,
    IdentificationProof,
    NodeProof,
    PeerProof,
    RequestMergeProof,

    TestLinkageProof,
    TemplateProof,
}

/// <summary>
/// Represents the base class of Proof.<br/>
/// This class holds an authentication key and its proof content.<br/>
/// To centralize serialization, add the derived Proof classes to ProofKey and apply the TinyhandUnion attribute to this class.
/// </summary>
// [TinyhandUnion((int)ProofKey.EvolProof2, typeof(EvolProof2))]
[TinyhandUnion((int)ProofKey.EvolProof, typeof(EvolProof))]
[TinyhandUnion((int)ProofKey.MergedProof, typeof(MergedProof))]
[TinyhandUnion((int)ProofKey.CertificateProof, typeof(CertificateProof))]
[TinyhandUnion((int)ProofKey.CredentialProof, typeof(CredentialProof))]
[TinyhandUnion((int)ProofKey.LinkProof, typeof(LinkProof))]
[TinyhandUnion((int)ProofKey.ValueProof, typeof(ValueProof))]
[TinyhandUnion((int)ProofKey.CreateCreditProof, typeof(CreateCreditProof))]
[TinyhandUnion((int)ProofKey.TransferProof, typeof(TransferProof))]
[TinyhandUnion((int)ProofKey.DimensionProof, typeof(DimensionProof))]
[TinyhandUnion((int)ProofKey.IdentificationProof, typeof(IdentificationProof))]
[TinyhandUnion((int)ProofKey.NodeProof, typeof(NodeProof))]
[TinyhandUnion((int)ProofKey.PeerProof, typeof(PeerProof))]
[TinyhandUnion((int)ProofKey.RequestMergeProof, typeof(RequestMergeProof))]
[TinyhandUnion((int)ProofKey.TestLinkageProof, typeof(TestLinkageProof))]
[TinyhandUnion((int)ProofKey.TemplateProof, typeof(TemplateProof))]
[TinyhandObject(ReservedKeyCount = Proof.ReservedKeyCount)]
public abstract partial class Proof : IEquatable<Proof>, ISignable
{
    public const int ReusableSignId = -1;

    /// <summary>
    /// The number of microseconds by which the expiration time is truncated.<br/>
    /// If the valid mics is less than or equal to this value, it will not be truncated.
    /// </summary>
    public const long TruncateExpirationMics = Mics.MicsPerDay;

    public const int TruncateExpirationSeconds = Seconds.SecondsPerDay;

    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 5;

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
    /// Gets or sets the identifier of the signature used for this proof.<br/>
    /// <see cref="ReusableSignId"/>: Reusable sign id.<br/>
    /// Other: Unique (not reusable) sign id.
    /// </summary>
    [Key(1)]
    public int SignId { get; protected set; }

    /// <summary>
    /// Gets or sets the signed time in microseconds.
    /// </summary>
    [Key(2)]
    public long SignedMics { get; protected set; }

    /// <summary>
    /// Gets or sets the validity period of the proof in seconds.
    /// </summary>
    [Key(3)]
    public int ValiditySeconds { get; protected set; }

    public long ExpirationMics => this.SignedMics + ((long)this.ValiditySeconds * Mics.MicsPerSecond);

    /// <summary>
    /// Gets the maximum valid microseconds.
    /// </summary>
    public virtual int MaxValiditySeconds => LpConstants.DefaultProofMaxValiditySeconds;

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

    /*/// <summary>
    /// Tries to get the value associated with the proof.
    /// </summary>
    /// <param name="value">The value associated with the proof, if available.</param>
    /// <returns><c>true</c> if the value is available; otherwise, <c>false</c>.</returns>
    public virtual bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = default;
        return false;
    }*/

    public virtual Point Account(ref SignaturePublicKey targetPublicKey)
    {
        return default;
    }

    /// <summary>
    /// Validates the proof.
    /// </summary>
    /// <param name="validationOptions">The validation options to apply during validation.</param>
    /// <returns><c>true</c> if the proof is valid; otherwise, <c>false</c>.</returns>
    public virtual bool Validate(ValidationOptions validationOptions)
    {
        if (this.ValiditySeconds == 0)
        {
            return false;
        }

        if (this.ValiditySeconds > this.MaxValiditySeconds)
        {
            return false;
        }

        if (!validationOptions.HasFlag(ValidationOptions.PreSign))
        {
            if (this.SignedMics == 0)
            {
                return false;
            }

            if (!validationOptions.HasFlag(ValidationOptions.IgnoreExpiration) &&
            !MicsRange.IsWithinMargin(Mics.FastCorrected, this.SignedMics, this.ExpirationMics))
            {
                return false;
            }
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
            this.ValiditySeconds == other.ValiditySeconds &&
            this.Signature.SequenceEqual(other.Signature) &&
            this.GetSignatureKey().Equals(other.GetSignatureKey());
    }

    public override string ToString() => this.ToString(default);

    public virtual string ToString(IConversionOptions? conversionOptions)
        => "abstract";

    public virtual bool SetSignature(SignaturePair signaturePair)
    {
        if (!signaturePair.IsValid || signaturePair.Index != 0)
        {
            return false;
        }

        this.Signature = signaturePair.Signature;
        return true;
    }

    public virtual bool PrepareForSigning(ref SignaturePublicKey publicKey, int validitySeconds)
    {
        if (this.SignId != Proof.ReusableSignId)
        {
            while (this.SignId == 0)
            {
                this.SignId = RandomVault.Default.NextInt32();
            }
        }

        this.SignedMics = Mics.GetCorrected();
        this.ValiditySeconds = Math.Min(validitySeconds, this.MaxValiditySeconds);

        return true;
    }
}
