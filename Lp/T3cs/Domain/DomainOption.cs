// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class DomainOption : IStringConvertible<DomainOption>
{
    #region FieldAndProperty

    [Key(0)]
    public Credit Credit { get; init; } = Credit.UnsafeConstructor();

    [Key(1)]
    public NetNode NetNode { get; init; } = new();

    [Key(2)]
    [MaxLength(LpConstants.MaxUrlLength)]
    public partial string Url { get; init; } = string.Empty;

    public static int MaxStringLength => Credit.MaxStringLength + 1 + NetNode.MaxStringLength + 1 + LpConstants.MaxUrlLength;

    #endregion

    public DomainOption(Credit credit, NetNode netNode, string url)
    {
        this.Credit = credit;
        this.NetNode = netNode;
        this.Url = url;
    }

    #region IStringConvertible

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out DomainOption @object, out int read, IConversionOptions? conversionOptions = null)
    {
        @object = default;
        read = 0;
        var s = source.Split(LpConstants.SeparatorSymbol);
        if (!s.MoveNext())
        {
            return false;
        }

        if (!Credit.TryParse(source[s.Current], out var credit, out _, conversionOptions))
        {
            return false;
        }

        if (!s.MoveNext())
        {
            return false;
        }

        if (!NetNode.TryParseWithAlternative(source[s.Current], out var netNode, out _, conversionOptions))
        {
            return false;
        }

        string url = string.Empty;
        if (s.MoveNext())
        {
            url = source[s.Current].ToString();
        }

        @object = new(credit, netNode, url);
        read = s.Current.End.Value;
        return true;
    }

    public int GetStringLength()
    {
        // return -1;
        var urlLength = string.IsNullOrEmpty(this.Url) ? 0 : 1 + this.Url.Length;
        return this.Credit.GetStringLength() + 1 + this.NetNode.GetStringLength() + urlLength;
    }

    public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = null)
    {
        if (!this.Credit.TryFormat(destination, out written, conversionOptions))
        {
            return false;
        }

        destination = destination.Slice(written);
        if (!BaseHelper.TryAppend(ref destination, ref written, LpConstants.SeparatorSymbol))
        {
            return false;
        }

        if (!this.NetNode.TryFormat(destination, out var w, conversionOptions))
        {
            return false;
        }

        written += w;
        destination = destination.Slice(w);

        if (string.IsNullOrEmpty(this.Url))
        {
            return true;
        }
        else
        {
            if (!BaseHelper.TryAppend(ref destination, ref written, LpConstants.SeparatorSymbol))
            {
                return false;
            }

            if (!BaseHelper.TryAppend(ref destination, ref written, this.Url.AsSpan()))
            {
                return false;
            }

            return true;
        }
    }

    #endregion
}
