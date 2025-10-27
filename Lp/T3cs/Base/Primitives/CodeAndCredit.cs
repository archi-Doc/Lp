// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using static Lp.Hashed;

namespace Lp.T3cs;

/// <summary>
/// Represents
/// </summary>
[TinyhandObject]
public sealed partial class CodeAndCredit : IValidatable, IEquatable<CodeAndCredit>, IStringConvertible<Credit>
{
    #region FieldAndProperty

    [Key(0)]
    public string Code { get; private set; } = string.Empty;

    [Key(1)]
    public Credit Credit { get; private set; } = Credit.UnsafeConstructor();

    #endregion

    #region IStringConvertible

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out Credit instance, out int read, IConversionOptions? conversionOptions = default)
    {// @Originator/Merger1+Merger2
        instance = default;
        read = 0;
        var span = source.Trim();

        if (span.Length < 1 || span[0] != LpConstants.CreditSymbol)
        {// @
            return false;
        }

        var initialLength = span.Length;
        span = span.Slice(1);
        if (!Identifier.TryParse(span, out var identifier, out var originatorRead, conversionOptions))
        {// Identifier
            return false;
        }

        span = span.Slice(originatorRead);

        if (span.Length == 0 || span[0] != LpConstants.MergerSymbol)
        {
            return false;
        }

        span = span.Slice(1);
        if (!SignaturePublicKey.TryParse(span, out var merger1, out read, conversionOptions))
        {
            return false;
        }

        // MaxMergersCode
        span = span.Slice(read);
        if (span.Length == 0)
        {// Single merger
            instance = new Credit(identifier, [merger1,]);
            read = initialLength - span.Length;
            return true;
        }

        if (span[0] != LpConstants.MergerSeparatorSymbol)
        {
            return false;
        }

        span = span.Slice(1);
        if (!SignaturePublicKey.TryParse(span, out var merger2, out read, conversionOptions))
        {
            return false;
        }

        span = span.Slice(read);
        if (span.Length == 0)
        {// Two merger2
            instance = new Credit(identifier, [merger1, merger2,]);
            read = initialLength - span.Length;
            return true;
        }

        if (span[0] != LpConstants.MergerSeparatorSymbol)
        {
            return false;
        }

        span = span.Slice(1);
        if (!SignaturePublicKey.TryParse(span, out var merger3, out read, conversionOptions))
        {
            return false;
        }

        span = span.Slice(read);
        if (span.Length == 0)
        {// Three Mergers
            instance = new Credit(identifier, [merger1, merger2, merger3,]);
            read = initialLength - span.Length;
            return true;
        }

        return false;
    }

    public static int MaxStringLength => (1 + SignaturePublicKey.MaxStringLength) * (2 + LpConstants.MaxMergers); // @Originator/Merger1+Merger2

    public int GetStringLength()
    {
        var length = 1 + this.Identifier.GetStringLength(); // + 1 + this.Standard.GetStringLength(); // @Originator:Standard/Merger1+Merger2
        foreach (var x in this.Mergers)
        {
            length += 1 + x.GetStringLength();
        }

        return length;
    }

    public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = default)
    {
        written = 0;
        var length = this.GetStringLength();
        if (destination.Length < length)
        {
            return false;
        }

        var span = destination;
        span[0] = LpConstants.CreditSymbol;
        span = span.Slice(1);
        written += 1;

        if (!this.Identifier.TryFormat(span, out var w, conversionOptions))
        {
            return false;
        }

        span = span.Slice(w);
        written += w;

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
                span[0] = LpConstants.MergerSymbol;
                span = span.Slice(1);
                written += 1;
            }
            else
            {
                span[0] = LpConstants.MergerSeparatorSymbol;
                span = span.Slice(1);
                written += 1;
            }

            if (!x.TryFormat(span, out w, conversionOptions))
            {
                return false;
            }

            span = span.Slice(w);
            written += w;
        }

        return true;
    }

    #endregion

    public CodeAndCredit(string code, Credit credit)
    {
        this.Code = code;
        this.Credit = credit;
    }

    public bool Validate()
    {
        return this.Credit.Validate();
    }

    public bool Equals(CodeAndCredit? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Code == other.Code &&
            this.Credit.Equals(other.Credit);
    }

    public override int GetHashCode()
    {
        return this.Credit.GetHashCode();
    }

    public override string ToString() => this.ConvertToString();

    public string ToString(IConversionOptions? conversionOptions) => this.ConvertToString(conversionOptions);
}
