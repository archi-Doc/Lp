// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

[TinyhandObject]
public partial class EngageProof : Proof
{
    public EngageProof()
    {
    }

    [Key(5)]
    public long Salt { get; protected set; }
}

/// <summary>
/// Represents a proof object.
/// </summary>
[TinyhandUnion(0, typeof(EngageProof))]
public abstract partial class Proof // : IValidatable, IEquatable<Proof>
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

    [Key(0)]
    public Proof? InnerProof { get; protected set; }

    // [Key(1)]
    // public Kind ProofKind { get; protected set; }

    [Key(1)]
    public PublicKey PublicKey { get; protected set; }

    [Key(2)]
    public byte[] Sign { get; protected set; } = Array.Empty<byte>();

    [Key(3)]
    public long ExpirationMics { get; protected set; }

    [Key(4)]
    public long Fee { get; protected set; }

    /*public bool Validate()
    {
        if (!this.ProofKey.Validate())
        {
            return false;
        }
        else if (this.Point < Value.MinPoint || this.Point > Value.MaxPoint)
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
        else if (!this.ProofKey.Equals(other.ProofKey))
        {
            return false;
        }
        else if (this.Point != other.Point)
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(this.Owner);
        hash.Add(this.Point);
        hash.Add(this.Credit);

        return hash.ToHashCode();
    }*/
}
