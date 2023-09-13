// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Represents a proof object.
/// </summary>
[TinyhandObject]
public sealed partial class Proof : IValidatable, IEquatable<Proof>
{
    public static readonly Proof Default = new();

    public enum Kind
    {
        CreateCredit,
        Transfer,
        Change,
        CloseAccount,
    }

    public Proof()
    {
    }

    [Key(0)]
    public Proof? InnerProof { get; private set; }

    [Key(1)]
    public Kind ProofKind { get; private set; }

    [Key(2)]
    public PublicKey ProofKey { get; private set; }

    [Key(3)]
    public byte[] ProofSign { get; private set; } = Array.Empty<byte>();

    [Key(4)]
    public long Point { get; private set; }

    public bool Validate()
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
        /*hash.Add(this.Owner);
        hash.Add(this.Point);
        hash.Add(this.Credit);*/

        return hash.ToHashCode();
    }
}
