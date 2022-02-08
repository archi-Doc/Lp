// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

[TinyhandObject]
public readonly partial struct PrimarySecondaryIdentifier : IEquatable<PrimarySecondaryIdentifier>, IComparable<PrimarySecondaryIdentifier>
{
    public PrimarySecondaryIdentifier(Identifier primaryId, Identifier secondaryId)
    {
        this.PrimaryId = primaryId;
        this.SecondaryId = secondaryId;
    }

    public PrimarySecondaryIdentifier()
    {
        this.PrimaryId = default; // Identifier.Zero; ! Must be default since TinyhandSerializer might modify Identifier.Zero.
        this.SecondaryId = default;
    }

    [Key(0)]
    public readonly Identifier PrimaryId;

    [Key(1)]
    public readonly Identifier SecondaryId;

    public bool Equals(PrimarySecondaryIdentifier other)
    {
        if (!this.PrimaryId.Equals(other.PrimaryId))
        {
            return false;
        }

        return this.SecondaryId.Equals(other.SecondaryId);
    }

    public override int GetHashCode()
    {
        return (int)(this.PrimaryId.Id0 ^ System.Numerics.BitOperations.RotateLeft(this.SecondaryId.Id0, 32));
    }

    public override string ToString()
    {
        return $"Primary {this.PrimaryId.Id0:D4} Secondary {this.SecondaryId.Id0:D4} ";
    }

    public int CompareTo(PrimarySecondaryIdentifier other)
    {
        var cmp = this.PrimaryId.CompareTo(other.PrimaryId);
        if (cmp != 0)
        {
            return cmp;
        }

        return this.SecondaryId.CompareTo(other.SecondaryId);
    }
}
