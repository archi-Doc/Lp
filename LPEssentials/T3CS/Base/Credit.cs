// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Credit (Originator/Merger:Standard).
/// </summary>
[TinyhandObject]
public sealed partial class Credit : IValidatable, IEquatable<Credit>
{
    public const int MaxMergers = 4;
    public static readonly Credit Default = new();

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

    [Key(1, AddProperty = "Mergers")]
    [MaxLength(MaxMergers)]
    private PublicKey[] mergers = Array.Empty<PublicKey>();

    [Key(2)]
    public PublicKey Standard { get; private set; } = default!;

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
        else if (!this.Standard.Validate())
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

        if (!this.Standard.Equals(other.Standard))
        {
            return false;
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

        hash.Add(this.Standard);
        return hash.ToHashCode();
    }
}
