// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using static Lp.Hashed;

namespace Lp.T3cs;

/// <summary>
/// Represents a credit information (@Originator/Merger1+Merger2).
/// </summary>
[TinyhandObject]
public sealed partial class Credit : IValidatable, IEquatable<Credit>, IStringConvertible<Credit>
{
    public static readonly Credit Default = Credit.UnsafeConstructor();
    public static readonly Credit Max = new Credit(MaxHelper.Identifier, MaxHelper.Merger);
    public static readonly int MaxBinarySize;

    public static bool TryCreate(Identifier identifier, SignaturePublicKey[] mergers, [MaybeNullWhen(false)] out Credit credit)
    {
        if (mergers.Length == 0 || mergers.Length > LpConstants.MaxMergers)
        {
            credit = default;
            return false;
        }

        credit = new(identifier, mergers);
        return true;
    }

    #region FieldAndProperty

    [Key(0)]
    public Identifier Identifier { get; private set; } = default;

    [Key(1)]
    [MaxLength(LpConstants.MaxMergers)]
    public partial SignaturePublicKey[] Mergers { get; private set; } = [];

    public int MergerCount => this.Mergers.Length;

    public SignaturePublicKey PrimaryMerger => this.Mergers.Length > 0 ? this.Mergers[0] : default;

    #endregion

    public static bool TryCreate(CreditIdentity creditIdentity, [MaybeNullWhen(false)] out Credit credit)
    {
        if (!creditIdentity.Validate())
        {
            credit = default;
            return false;
        }

        var obj = new Credit(creditIdentity.GetIdentifier(), creditIdentity.Mergers);

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

    public Credit(Identifier identifier, SignaturePublicKey[] mergers)
    {
        if (mergers.Length == 0 || mergers.Length > LpConstants.MaxMergers)
        {
            throw new ArgumentOutOfRangeException(nameof(mergers));
        }

        this.Identifier = identifier;
        this.Mergers = mergers;
    }

    public ulong GetDomainHash()
        => this.Identifier.Id1;

    public bool Validate()
    {
        if (this.Mergers == null)
        {
            return false;
        }

        var count = this.Mergers.Length; // MaxMergersCode
        if (count == 1)
        {
            return this.Mergers[0].Validate();
        }
        else if (count == 2)
        {
            return this.Mergers[0].Validate() && this.Mergers[1].Validate();
        }
        else if (count == 3)
        {
            return this.Mergers[0].Validate() && this.Mergers[1].Validate() && this.Mergers[2].Validate();
        }

        return false;
    }

    public int GetMergerIndex(ref SignaturePublicKey publicKey)
    {
        var count = this.Mergers.Length; // MaxMergersCode
        if (count == 0)
        {
            return -1;
        }
        else if (this.Mergers[0].Equals(ref publicKey))
        {
            return 0;
        }

        if (count == 1)
        {
            return -1;
        }
        else if (this.Mergers[1].Equals(ref publicKey))
        {
            return 1;
        }

        if (count == 2)
        {
            return -1;
        }
        else if (this.Mergers[2].Equals(ref publicKey))
        {
            return 2;
        }

        return -1;
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
        /*if (this.MergerCount == 1)
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
        }*/

        return (int)this.Identifier.Id0;
    }

    public override string ToString() => this.ConvertToString();

    public string ToString(IConversionOptions? conversionOptions) => this.ConvertToString(conversionOptions);
}
