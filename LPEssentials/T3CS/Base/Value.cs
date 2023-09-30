// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Represents a value (Owner#Point@Originator:Standard/Mergers).
/// </summary>
[TinyhandObject]
public sealed partial class Value : IValidatable, IEquatable<Value>
{
    public const long MaxPoint = 1_000_000_000_000_000_000; // k, m, g, t, p, e, 1z
    public const long MinPoint = 1; // -MaxPoint;

    public Value()
    {
    }

    public Value(long point, SignaturePublicKey originator, SignaturePublicKey[] mergers)
    {
        this.Point = point;
        this.Credit = new(originator, mergers);

        if (!this.Validate())
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    [Key(0)]
    public SignaturePublicKey Owner { get; private set; }

    [Key(1)]
    public long Point { get; private set; }

    [Key(2)]
    public Credit Credit { get; private set; } = Credit.Default;

    public bool Validate()
    {
        if (!this.Owner.Validate())
        {
            return false;
        }
        else if (this.Point < MinPoint || this.Point > MaxPoint)
        {
            return false;
        }
        else if (!this.Credit.Validate())
        {
            return false;
        }

        return true;
    }

    public bool Equals(Value? other)
    {
        if (other == null)
        {
            return false;
        }
        else if (!this.Owner.Equals(other.Owner))
        {
            return false;
        }
        else if (this.Point != other.Point)
        {
            return false;
        }
        else if (!this.Credit.Equals(other.Credit))
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
    }
}
