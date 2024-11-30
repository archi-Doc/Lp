// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents a credit information (@Originator:Standard/Merger1+Merger2).
/// </summary>
[TinyhandObject]
public sealed partial class Credit : IValidatable, IEquatable<Credit>, IStringConvertible<Credit>
{
    public const char CreditSymbol = '@';
    // public const char StandardSymbol = ':';
    public const char MergerSymbol = '/';
    public const char MergerSeparatorSymbol = '+';
    public const int MaxMergers = 3; // MaxMergersCode
    public static readonly Credit Default = new();

    public static bool TryCreate(SignaturePublicKey originator, SignaturePublicKey[] mergers, [MaybeNullWhen(false)] out Credit credit)
    {
        var obj = new Credit();
        obj.Originator = originator;
        obj.mergers = mergers;

        if (obj.Validate())
        {
            credit = obj;
            return true;
        }
        else
        {
            credit = default;
            return false;
        }
    }

    #region IStringConvertible

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out Credit instance)
    {// @Originator/Merger1+Merger2
        instance = default;
        var span = source.Trim();

        if (span.Length < 1 || span[0] != CreditSymbol)
        {// @
            return false;
        }

        span = span.Slice(1);
        if (!SignaturePublicKey.TryParse(span, out var originator, out var parsedLength))
        {// Originator
            return false;
        }

        span = span.Slice(parsedLength);

        if (span.Length == (1 + SignaturePublicKey.MaxStringLength))
        {
            if (span[0] != MergerSymbol)
            {
                return false;
            }

            span = span.Slice(1);
            if (!SignaturePublicKey.TryParse(span, out var merger1))
            {
                return false;
            }

            instance = new Credit();
            instance.Originator = originator;
            instance.mergers = [merger1,];
            return true;
        }
        else if (span.Length == (1 + SignaturePublicKey.MaxStringLength) * 2)
        {
            if (span[0] != MergerSymbol)
            {
                return false;
            }

            span = span.Slice(1);
            if (!SignaturePublicKey.TryParse(span, out var merger1))
            {
                return false;
            }

            if (span[0] != MergerSeparatorSymbol)
            {
                return false;
            }

            span = span.Slice(1);
            if (!SignaturePublicKey.TryParse(span, out var merger2))
            {
                return false;
            }

            instance = new Credit();
            instance.Originator = originator;
            instance.mergers = [merger1, merger2,];
            return true;
        }
        else if (span.Length == (1 + SignaturePublicKey.MaxStringLength) * 3)
        {
            if (span[0] != MergerSymbol)
            {
                return false;
            }

            span = span.Slice(1);
            if (!SignaturePublicKey.TryParse(span, out var merger1))
            {
                return false;
            }

            if (span[0] != MergerSeparatorSymbol)
            {
                return false;
            }

            span = span.Slice(1);
            if (!SignaturePublicKey.TryParse(span, out var merger2))
            {
                return false;
            }

            if (span[0] != MergerSeparatorSymbol)
            {
                return false;
            }

            span = span.Slice(1);
            if (!SignaturePublicKey.TryParse(span, out var merger3))
            {
                return false;
            }

            instance = new Credit();
            instance.Originator = originator;
            instance.mergers = [merger1, merger2, merger3,];
            return true;
        }

        return false;
    }

    public static int MaxStringLength => (1 + SignaturePublicKey.MaxStringLength) * (2 + MaxMergers); // @Originator/Merger1+Merger2

    public int GetStringLength()
    {
        var length = 1 + this.Originator.GetStringLength(); // + 1 + this.Standard.GetStringLength(); // @Originator:Standard/Merger1+Merger2
        foreach (var x in this.mergers)
        {
            length += 1 + x.GetStringLength();
        }

        return length;
    }

    public bool TryFormat(Span<char> destination, out int written)
    {
        written = 0;
        var length = this.GetStringLength();
        if (destination.Length < length)
        {
            return false;
        }

        var span = destination;
        span[0] = CreditSymbol;
        span = span.Slice(1);

        if (!this.Originator.TryFormat(span, out var w))
        {
            return false;
        }

        span = span.Slice(w);

        /*span[0] = StandardSymbol;
        span = span.Slice(1);
        if (!this.Standard.TryFormat(span, out w))
        {
            return false;
        }

        span = span.Slice(w);*/

        var isFirst = true;
        foreach (var x in this.mergers)
        {
            if (isFirst)
            {
                isFirst = false;
                span[0] = MergerSymbol;
                span = span.Slice(1);
            }
            else
            {
                span[0] = MergerSeparatorSymbol;
                span = span.Slice(1);
            }

            if (!x.TryFormat(span, out w))
            {
                return false;
            }

            span = span.Slice(w);
        }

        written = length;
        return true;
    }

    #endregion

    public Credit()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    public SignaturePublicKey Originator { get; private set; } = default!;

    [Key(1, AddProperty = "Mergers")]
    [MaxLength(MaxMergers)]
    private SignaturePublicKey[] mergers = Array.Empty<SignaturePublicKey>();

    // [Key(2)]
    // public SignaturePublicKey Standard { get; private set; } = default!;

    public int MergerCount => this.mergers.Length;

    #endregion

    public bool Validate()
    {
        if (!this.Originator.Validate())
        {
            return false;
        }

        if (this.mergers == null ||
            this.mergers.Length == 0 ||
            this.mergers.Length > MaxMergers)
        {
            return false;
        }

        for (var i = 0; i < this.mergers.Length; i++)
        {
            if (!this.mergers[i].Validate())
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

        /*if (!this.Standard.Equals(other.Standard))
        {
            return false;
        }*/

        return true;
    }

    public override int GetHashCode()
    {
        // MaxMergersCode
        if (this.MergerCount == 1)
        {
            return HashCode.Combine(this.Originator, this.mergers[0]);
        }
        else if (this.MergerCount == 2)
        {
            return HashCode.Combine(this.Originator, this.mergers[0], this.mergers[1]);
        }
        else if (this.MergerCount == 3)
        {
            return HashCode.Combine(this.Originator, this.mergers[0], this.mergers[1], this.mergers[2]);
        }
        else
        {
            return HashCode.Combine(this.Originator);
        }
    }

    public override string ToString()
        => this.ConvertToString();
}
