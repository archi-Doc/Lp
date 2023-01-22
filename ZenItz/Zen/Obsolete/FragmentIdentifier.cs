// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz.Obsolete;

[TinyhandObject]
public readonly partial struct FragmentIdentifier : IEquatable<FragmentIdentifier>, IComparable<FragmentIdentifier>
{
    public FragmentIdentifier(TIdentifier id, bool isPrimary)
    {
        this.Id = id;
        this.IsPrimary = isPrimary;
    }

    public FragmentIdentifier()
    {
        this.Id = default; // TIdentifier.Zero; ! Must be default since TinyhandSerializer might modify TIdentifier.Zero.
        this.IsPrimary = true;
    }

    [Key(0)]
    public readonly TIdentifier Id;

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
