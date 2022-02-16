// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

[TinyhandObject]
public readonly partial struct FragmentIdentifier : IEquatable<FragmentIdentifier>, IComparable<FragmentIdentifier>
{
    public FragmentIdentifier(Identifier id, bool isPrimary)
    {
        this.Id = id;
        this.IsPrimary = isPrimary;
    }

    public FragmentIdentifier()
    {
        this.Id = default; // Identifier.Zero; ! Must be default since TinyhandSerializer might modify Identifier.Zero.
        this.IsPrimary = true;
    }

    [Key(0)]
    public readonly Identifier Id;

    [Key(1)]
    public readonly bool IsPrimary;

    public bool Equals(FragmentIdentifier other)
    {
        if (!this.Id.Equals(other.Id))
        {
            return false;
        }

        return this.IsPrimary == other.IsPrimary;
    }

    public override int GetHashCode()
    {
        return (int)this.Id.Id0 ^ (this.IsPrimary ? 0 : 1);
    }

    public override string ToString()
    {
        return $"Id {this.Id.Id0:D4} Primary {this.IsPrimary.ToString()} ";
    }

    public int CompareTo(FragmentIdentifier other)
    {
        var cmp = this.Id.CompareTo(other.Id);
        if (cmp != 0)
        {
            return cmp;
        }

        return this.IsPrimary.CompareTo(other.IsPrimary);
    }
}
