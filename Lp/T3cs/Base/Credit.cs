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

    #region FieldAndProperty

    [Key(0)]
    public Identifier Identifier { get; private set; } = default!;

    [Key(1)]
    [MaxLength(MaxMergers)]
    public partial SignaturePublicKey[] Mergers { get; private set; } = [];

    // [Key(2)]
    // public SignaturePublicKey Standard { get; private set; } = default!;

    public int MergerCount => this.Mergers.Length;

    #endregion

    public static bool TryCreate(Identity creditIdentity, [MaybeNullWhen(false)] out Credit credit)
    {
        var obj = new Credit();
        obj.Identifier = creditIdentity.GetIdentifier();
        obj.Mergers = creditIdentity.Mergers;

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

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out Credit instance, out int read)
    {// @Originator/Merger1+Merger2
        instance = default;
        read = 0;
        var span = source.Trim();

        if (span.Length < 1 || span[0] != CreditSymbol)
        {// @
            return false;
        }

        var initialLength = span.Length;
        span = span.Slice(1);
        if (!Identifier.TryParse(span, out var identifier, out var originatorRead))
        {// Identifier
            return false;
        }

        span = span.Slice(originatorRead);

        if (span.Length == 0 || span[0] != MergerSymbol)
        {
            return false;
        }

        span = span.Slice(1);
        if (!SignaturePublicKey.TryParse(span, out var merger1, out read))
        {
            return false;
        }

        // MaxMergersCode
        span = span.Slice(read);
        if (span.Length == 0)
        {// Single merger
            instance = new Credit();
            instance.Identifier = identifier;
            instance.Mergers = [merger1,];
            read = initialLength - span.Length;
            return true;
        }

        if (span[0] != MergerSeparatorSymbol)
        {
            return false;
        }

        span = span.Slice(1);
        if (!SignaturePublicKey.TryParse(span, out var merger2, out read))
        {
            return false;
        }

        span = span.Slice(read);
        if (span.Length == 0)
        {// Two merger2
            instance = new Credit();
            instance.Identifier = identifier;
            instance.Mergers = [merger1, merger2,];
            read = initialLength - span.Length;
            return true;
        }

        if (span[0] != MergerSeparatorSymbol)
        {
            return false;
        }

        span = span.Slice(1);
        if (!SignaturePublicKey.TryParse(span, out var merger3, out read))
        {
            return false;
        }

        span = span.Slice(read);
        if (span.Length == 0)
        {// Three Mergers
            instance = new Credit();
            instance.Identifier = identifier;
            instance.Mergers = [merger1, merger2, merger3,];
            read = initialLength - span.Length;
            return true;
        }

        return false;
    }

    public static int MaxStringLength => (1 + SignaturePublicKey.MaxStringLength) * (2 + MaxMergers); // @Originator/Merger1+Merger2

    public int GetStringLength()
    {
        var length = 1 + this.Identifier.GetStringLength(); // + 1 + this.Standard.GetStringLength(); // @Originator:Standard/Merger1+Merger2
        foreach (var x in this.Mergers)
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

        if (!this.Identifier.TryFormat(span, out var w))
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
        foreach (var x in this.Mergers)
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

    public bool Validate()
    {
        if (this.Mergers == null ||
            this.Mergers.Length == 0 ||
            this.Mergers.Length > MaxMergers)
        {
            return false;
        }

        for (var i = 0; i < this.Mergers.Length; i++)
        {
            if (!this.Mergers[i].Validate())
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
        else if (!this.Identifier.Equals(other.Identifier))
        {
            return false;
        }
        else if (this.Mergers.Length != other.Mergers.Length)
        {
            return false;
        }

        for (var i = 0; i < this.Mergers.Length; i++)
        {
            if (!this.Mergers[i].Equals(other.Mergers[i]))
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
            return HashCode.Combine(this.Identifier, this.Mergers[0]);
        }
        else if (this.MergerCount == 2)
        {
            return HashCode.Combine(this.Identifier, this.Mergers[0], this.Mergers[1]);
        }
        else if (this.MergerCount == 3)
        {
            return HashCode.Combine(this.Identifier, this.Mergers[0], this.Mergers[1], this.Mergers[2]);
        }
        else
        {
            return HashCode.Combine(this.Identifier);
        }
    }

    public override string ToString()
        => this.ConvertToString();
}
