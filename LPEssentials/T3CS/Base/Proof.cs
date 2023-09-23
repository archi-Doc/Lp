// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

[TinyhandObject]
public partial class EngageProof : Proof
{
    public EngageProof()
    {
    }

    [Key(5)]
    public PublicKey ProofKey { get; set; }

    // public long Salt { get; protected set; }
}

/// <summary>
/// Represents a proof object.
/// </summary>
// [TinyhandUnion(0, typeof(EngageProof))]
[TinyhandObject]
public partial class Proof : IVerifiable, IEquatable<Proof>
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
    public long ProofMics { get; protected set; }

    [Key(1)]
    public byte[] Sign { get; protected set; } = Array.Empty<byte>();

    /*[Key(0)]
    public Proof? InnerProof { get; protected set; }

    [Key(1)]
    public Kind ProofKind { get; protected set; }

    [Key(1)]
    public PublicKey PublicKey { get; protected set; }*/

    [Key(3)]
    public long ExpirationMics { get; protected set; }

    public virtual PublicKey ProofKey { get; }

    #endregion

    public bool Validate()
    {
        if (this.ProofMics == 0)
        {
            return false;
        }

        return true;
    }

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }

        var serialize = (ITinyhandSerialize)this;
        return this.VerifySign(0, this.ProofKey, this.Sign);
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
}
