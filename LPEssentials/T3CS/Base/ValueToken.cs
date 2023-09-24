// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Represents a value token (Owner#Point@Originator:Standard/Mergers + Signature).
/// </summary>
[TinyhandObject]
public sealed partial class ValueToken : IValidatable, IEquatable<ValueToken>
{
    public const long MaximumMics = Mics.MicsPerYear * 1;
    public static readonly ValueToken Default = new();

    public ValueToken()
    {
    }

    [Key(0)]
    public PublicKey Owner { get; private set; }

    [Key(1)]
    public long Point { get; private set; }

    [Key(2)]
    public Credit Credit { get; private set; } = Credit.Default;

    [Key(3)]
    public Signature Signature { get; private set; }

    public bool Validate()
    {
        if (!this.Owner.Validate(KeyClass.T3CS_Signature))
        {
            return false;
        }
        else if (this.Point < Value.MinPoint || this.Point > Value.MaxPoint)
        {
            return false;
        }
        else if (!this.Credit.Validate())
        {
            return false;
        }

        return true;
    }

    public bool Equals(ValueToken? other)
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
        else if (!this.Signature.Equals(other.Signature))
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
