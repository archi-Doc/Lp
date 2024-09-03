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
    public const long MaxExpirationMics = Mics.MicsPerDay * 10;
    public const long TruncateExpirationMics = Mics.MicsPerDay;
    public const int ReservedKeyCount = 4;

    public Proof()
    {
    }

    #region FieldAndProperty

    SignaturePublicKey IVerifiable.PublicKey => this.GetPublicKey();

    // [Key(0)] -> ProofAndPublicKey, ProofAndCredit, ProofAndValue
    // public SignaturePublicKey PublicKey { get; }

    [Key(1, Level = 1)]
    public byte[] Signature { get; protected set; } = Array.Empty<byte>();

    [Key(2)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public long VerificationMics { get; protected set; }

    [Key(3)]
    public long ExpirationMics { get; protected set; }

    #endregion

    public abstract SignaturePublicKey GetPublicKey();

    public virtual bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = default;
        return false;
    }

    public virtual bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = default;
        return false;
    }

    public virtual bool Validate()
    {
        if (this.VerificationMics == 0)
        {
            return false;
        }

        return true;
    }

    public bool Equals(Proof? other)
    {
        if (other == null)
        {
            return false;
        }
        else if (this.VerificationMics != other.VerificationMics)
        {
            return false;
        }

        /*else if (this.Point != other.Point)
        {
            return false;
        }*/

        return true;
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
