// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using static Lp.Hashed;

namespace Lp.T3cs;

/// <summary>
/// Represents Code (Vault/Authority/Private key) and Credit.
/// </summary>
[TinyhandObject]
public sealed partial class CodeAndCredit : IValidatable, IEquatable<CodeAndCredit>, IStringConvertible<CodeAndCredit>
{
    public static readonly int MaxCodeLength = SeedKeyHelper.MaxPrivateKeyLengthInBase64;

    #region FieldAndProperty

    [Key(0)]
    public string Code { get; private set; } = string.Empty;

    [Key(1)]
    public Credit Credit { get; private set; } = Credit.UnsafeConstructor();

    #endregion

    #region IStringConvertible

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out CodeAndCredit instance, out int read, IConversionOptions? conversionOptions = default)
    {// Code@Credit
        var idx = source.IndexOf(LpConstants.CreditSymbol);
        if (idx < 0)
        {
            instance = default;
            read = 0;
            return false;
        }

        var codeSpan = source.Slice(0, idx);
        source = source.Slice(idx);
        if (Credit.TryParse(source, out var credit, out var r, conversionOptions))
        {
            instance = new CodeAndCredit(codeSpan.ToString(), credit);
            read = idx + r;
            return true;
        }
        else
        {
            instance = default;
            read = 0;
            return false;
        }
    }

    public static int MaxStringLength => MaxCodeLength + Credit.MaxStringLength;

    public int GetStringLength()
    {
        return this.Code.Length + this.Credit.GetStringLength();
    }

    public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = default)
    {
        var span = destination;
        written = 0;
        var length = this.GetStringLength();
        if (span.Length < length)
        {
            return false;
        }

        this.Code.AsSpan().CopyTo(span);
        span = span.Slice(this.Code.Length);

        if (!this.Credit.TryFormat(span, out var w, conversionOptions))
        {
            written = 0;
            return false;
        }

        written = this.Code.Length + w;
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
        return this.Code.Length <= MaxCodeLength &&
            this.Credit.Validate();
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
