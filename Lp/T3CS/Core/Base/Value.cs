// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents a value (Owner#Point@Originator:Standard/Mergers).
/// </summary>
[TinyhandObject]
public sealed partial class Value : IValidatable, IEquatable<Value>, IStringConvertible<Value>
{
    public const char PointSymbol = '#';
    public const int MaxPointLength = 19;
    public const long MaxPoint = 1_000_000_000_000_000_000; // k, m, g, t, p, e, 1z
    public const long MinPoint = 1; // -MaxPoint;

    public static bool TryCreate(SignaturePublicKey owner, Point point, Credit credit, [MaybeNullWhen(false)] out Value value)
    {
        var v = new Value();
        v.Owner = owner;
        v.Point = point;
        v.Credit = credit;

        if (v.Validate())
        {
            value = v;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    #region IStringConvertible

    public static int MaxStringLength => 1 + SignaturePublicKey.MaxStringLength + MaxPointLength + Credit.MaxStringLength; // Owner#Point + Credit

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out Value? instance)
    {// Owner#Point@Originator/Mergers
        instance = default;
        var span = source.Trim();

        var pointIndex = span.IndexOf(PointSymbol);
        if (pointIndex < 0)
        {
            return false;
        }

        var ownerSpan = span.Slice(0, pointIndex);
        if (ownerSpan.Length < SignaturePublicKey.MaxStringLength ||
            !SignaturePublicKey.TryParse(ownerSpan, out var owner))
        {
            return false;
        }

        span = span.Slice(pointIndex + 1);
        var creditIndex = span.IndexOf(Credit.CreditSymbol);
        if (creditIndex < 0)
        {
            return false;
        }

        var pointSpan = span.Slice(0, creditIndex);
        if (!Point.TryParse(pointSpan, out var point))
        {
            return false;
        }

        if (!Credit.TryParse(span.Slice(creditIndex), out var credit))
        {
            return false;
        }

        if (!Value.TryCreate(owner, point, credit, out var value))
        {
            return false;
        }

        instance = value;
        return true;
    }

    public int GetStringLength() => -1;

    public bool TryFormat(Span<char> destination, out int written)
    {
        written = 0;
        if (destination.Length < MaxStringLength)
        {
            return false;
        }

        var span = destination;
        if (!this.Owner.TryFormat(span, out var ownerWritten))
        {
            return false;
        }

        span = span.Slice(ownerWritten);
        span[0] = PointSymbol;
        span = span.Slice(1);
        if (!this.Point.TryFormat(span, out var pointWritten))
        {
            return false;
        }

        span = span.Slice(pointWritten);

        if (!this.Credit.TryFormat(span, out var creditWritten))
        {
            return false;
        }

        written = ownerWritten + 1 + pointWritten + creditWritten;
        return true;
    }

    #endregion

    #region FieldAndProperty

    [Key(0)]
    public SignaturePublicKey Owner { get; private set; }

    [Key(1)]
    public Point Point { get; private set; }

    [Key(2)]
    public Credit Credit { get; private set; } = Credit.Default;

    #endregion

    public Value()
    {
    }

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

    public override string ToString()
        => this.ConvertToString();
}
