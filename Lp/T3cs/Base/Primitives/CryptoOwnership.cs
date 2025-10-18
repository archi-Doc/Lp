// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents an ownership with CryptoKey (CryptoKey@Ideitifier/Mergers).
/// </summary>
[TinyhandObject]
public sealed partial record class CryptoOwnership : IValidatable, IEquatable<CryptoOwnership>, IStringConvertible<CryptoOwnership>
{
    #region FieldAndProperty

    [Key(0)]
    public CryptoKey Owner { get; private set; } = CryptoKey.UnsafeConstructor();

    [Key(1)]
    public Credit Credit { get; private set; } = Credit.UnsafeConstructor();

    // [Key(2)]
    // public Point Point { get; private set; }

    #endregion

    public static bool TryCreate(CryptoKey owner, Credit credit, [MaybeNullWhen(false)] out CryptoOwnership value)
    {
        var v = CryptoOwnership.UnsafeConstructor();
        v.Owner = owner;
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

    public static int MaxStringLength => CryptoKey.MaxStringLength + Credit.MaxStringLength; // CryptoKey + Credit

    /*public static async Task<CryptoOwnership?> TryParseAuthority(AuthorityControl authorityControl, ReadOnlyMemory<char> source, IConversionOptions? conversionOptions = default)
    {
        if (source.Length == 0)
        {
            return default;
        }

        var span = source.Span;
        if (span[0] != SeedKeyHelper.PublicKeyOpenBracket)
        {// CryptoKey@Ideitifier/Mergers
            var index = span.IndexOf(LpConstants.CreditSymbol);
            if (index < 0)
            {
                return default;
            }

            var authorityName = source.Slice(0, index).ToString();
            var authority = await authorityControl.GetAuthority(authorityName).ConfigureAwait(false);
            if (authority is null)
            {
                return default;
            }

            span = span.Slice(index + 1);
            if (!Credit.TryParse(span, out var credit, out var read, conversionOptions))
            {
                return default;
            }

            span = span.Slice(read);
            if (!CryptoOwnership.TryCreate(cryptoKey, credit, out var value))
            {
                return false;
            }
        }
        else
        {
            TryParse(span, out var instance, out var read, conversionOptions);
            return instance;
        }
    }*/

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out CryptoOwnership? instance, out int read, IConversionOptions? conversionOptions = default)
    {// CryptoKey@Ideitifier/Mergers
        var span = source;
        instance = default;
        read = 0;

        if (!CryptoKey.TryParse(span, out var cryptoKey, out read, conversionOptions))
        {
            return false;
        }

        span = span.Slice(read);
        if (!Credit.TryParse(span, out var credit, out read, conversionOptions))
        {
            return false;
        }

        span = span.Slice(read);
        if (!CryptoOwnership.TryCreate(cryptoKey, credit, out var value))
        {
            return false;
        }

        instance = value;
        read = source.Length - span.Length;
        return true;
    }

    public int GetStringLength() => this.Owner.GetStringLength() + this.Credit.GetStringLength();

    public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = default)
    {
        written = 0;
        if (destination.Length < this.GetStringLength())
        {
            return false;
        }

        var span = destination;
        if (!this.Owner.TryFormat(span, out var ownerWritten, conversionOptions))
        {
            return false;
        }

        span = span.Slice(ownerWritten);
        if (!this.Credit.TryFormat(span, out var creditWritten, conversionOptions))
        {
            return false;
        }

        written = ownerWritten + creditWritten;
        return true;
    }

    #endregion

    public CryptoOwnership(CryptoKey owner, Credit credit)
    {
        this.Owner = owner;
        this.Credit = credit;
    }

    public bool Validate()
    {
        if (!this.Credit.Validate())
        {
            return false;
        }

        return true;
    }

    public bool Equals(CryptoOwnership? other)
    {
        if (other == null)
        {
            return false;
        }
        else if (!this.Owner.Equals(other.Owner))
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
        hash.Add(this.Credit);

        return hash.ToHashCode();
    }

    public override string ToString() => this.ConvertToString();

    public string ToString(IConversionOptions? conversionOptions) => this.ConvertToString(conversionOptions);
}
