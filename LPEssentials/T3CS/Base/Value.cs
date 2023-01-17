// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Immutable value object.
/// </summary>
[TinyhandObject]
public sealed partial class Value : IValidatable, IEquatable<Value>
{
    public const long MaxPoint = 1_000_000_000_000_000_000; // k, m, g, t, p, e, 1z
    public const long MinPoint = 0; // -MaxPoint;
    public const int MaxMergers = 4;

    public Value()
    {
    }

    public Value(long point, PublicKey originator, PublicKey[] mergers)
    {
        this.Point = point;
        this.Originator = originator;
        this.mergers = mergers;

        if (!this.Validate())
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    [Key(0)]
    public long Point { get; private set; }

    [Key(1)]
    public PublicKey Originator { get; private set; } = default!;

    [Key(2, PropertyName = "Mergers")]
    [MaxLength(MaxMergers)]
    private PublicKey[] mergers = default!;

    public bool Validate()
    {
        if (this.Point < MinPoint || this.Point > MaxPoint)
        {
            return false;
        }
        else if (!this.Originator.Validate())
        {
            return false;
        }
        else if (this.mergers == null ||
            this.mergers.Length == 0 ||
            this.mergers.Length > MaxMergers)
        {
            return false;
        }

        var keyVersion = this.Originator.KeyVersion;
        for (var i = 0; i < this.mergers.Length; i++)
        {
            if (this.mergers[i].KeyVersion != keyVersion || !this.mergers[i].Validate())
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(Value? other)
    {
        if (other == null)
        {
            return false;
        }
        else if (!this.Originator.Equals(other.Originator))
        {
            return false;
        }
        else if (this.mergers.Length != other.mergers.Length)
        {
            return false;
        }

        for (var i = 0; i < this.mergers.Length; i++)
        {
            if (!this.mergers[i].Equals(other.mergers[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(this.Point);
        hash.Add(this.Originator);

        foreach (var x in this.mergers)
        {
            hash.Add(x);
        }

        return hash.ToHashCode();
    }
}
