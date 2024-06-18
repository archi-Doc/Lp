// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.T3CS;

/// <summary>
/// Represents a proof object.
/// </summary>
// [TinyhandUnion(0, typeof(EngageProof))]
[TinyhandUnion(0, typeof(CreateCreditProof))]
[TinyhandUnion(1, typeof(EvolProof))]
[TinyhandUnion(2, typeof(TransferProof))]
[TinyhandUnion(3, typeof(DimensionProof))]
[TinyhandUnion(4, typeof(IdentificationProof))]
[TinyhandObject(ReservedKeys = 4)]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public abstract partial class Proof : IVerifiable, IEquatable<Proof>
{
    /*public enum Kind
    {
        CreateCredit,
        Engage,
        Transfer,
        Merge,
        CloseBorrower,
    }*/

    public Proof()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    public SignaturePublicKey PublicKey { get; protected set; }

    [Key(1, Level = 1)]
    public byte[] Signature { get; protected set; } = Array.Empty<byte>();

    [Key(2)]
    [Link(Primary = true, Type = ChainType.Ordered)]
    public long ProofMics { get; protected set; }

    // [Key(3)]
    // public long ExpirationMics { get; protected set; }

    #endregion

    public virtual bool Validate()
    {
        if (this.ProofMics == 0)
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
        else if (this.ProofMics != other.ProofMics)
        {
            return false;
        }

        /*else if (this.Point != other.Point)
        {
            return false;
        }*/

        return true;
    }

    internal void SetInformationInternal(SignaturePrivateKey privateKey, long proofMics)
    {
        this.PublicKey = privateKey.ToPublicKey();
        this.ProofMics = proofMics;
    }

    internal void SetSignInternal(byte[] sign)
    {
        this.Signature = sign;
    }
}
