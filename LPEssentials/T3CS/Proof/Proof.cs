// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

public static class ProofHelper
{
}

/// <summary>
/// Represents a proof object.
/// </summary>
// [TinyhandUnion(0, typeof(EngageProof))]
[TinyhandObject(ReservedKeys = 4)]
public abstract partial class Proof : IVerifiable, IEquatable<Proof>
{
    // public static readonly Proof Default = new();

    public enum Kind
    {
        CreateCredit,
        Engage,
        Transfer,
        Merge,
        CloseBorrower,
    }

    public Proof()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    public SignaturePublicKey PublicKey { get; protected set; }

    [Key(1, Level = 1)]
    public byte[] Signature { get; protected set; } = Array.Empty<byte>();

    [Key(2)]
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

    /*public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(this.Owner);
        hash.Add(this.Point);
        hash.Add(this.Credit);

        return hash.ToHashCode();
    }*/

    internal void SetInternal(SignaturePrivateKey privateKey, long proofMics)
    {
        this.PublicKey = privateKey.ToPublicKey();
        this.ProofMics = proofMics;
    }

    internal void SignInternal(byte[] sign)
    {
        this.Signature = sign;
    }
}
