// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace LP;

/// <summary>
/// Immutable credit object.
/// </summary>
[TinyhandObject]
public sealed partial class Credit : IValidatable, IEquatable<Credit>
{
    public const int MaxMergers = 4;

    public Credit()
    {
    }

    public Credit(PublicKey originator, PublicKey[] mergers)
    {
        this.Originator = originator;
        this.mergers = mergers;

        if (!this.Validate())
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    [Key(0)]
    public PublicKey Originator { get; private set; } = default!;

    [Key(1, PropertyName = "Mergers")]
    [MaxLength(MaxMergers)]
    private PublicKey[] mergers = Array.Empty<PublicKey>();

    public bool Validate()
    {
        if (!this.Originator.Validate())
        {
            return false;
        }
        else if (this.mergers == null ||
            this.mergers.Length == 0 ||
            this.mergers.Length > MaxMergers)
        {
            return false;
        }

        var keyType = this.Originator.KeyType;
        for (var i = 0; i < this.mergers.Length; i++)
        {
            if (this.mergers[i].KeyType != keyType || !this.mergers[i].Validate())
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(Credit? other)
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
        hash.Add(this.Originator);

        foreach (var x in this.mergers)
        {
            hash.Add(x);
        }

        return hash.ToHashCode();
    }
}
