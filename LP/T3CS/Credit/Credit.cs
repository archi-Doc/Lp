// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.T3CS;

/// <summary>
/// Represents a credit information (@Originator:Standard/Merger1+Merger2).
/// </summary>
[TinyhandObject]
public sealed partial class Credit : IValidatable, IEquatable<Credit>, IStringConvertible<Credit>
{
    public const char CreditSymbol = '@';
    public const char StandardSymbol = ':';
    public const char MergerSymbol = '/';
    public const char MergerSeparator = '+';
    public const int MaxMergers = 3;
    public static readonly Credit Default = new();

    #region IStringConvertible

    public static bool TryParse(ReadOnlySpan<char> source, out Credit? instance)
    {
        instance = default;
        var span = source.Trim();

        if (span.Length < 1 || span[0] != CreditSymbol)
        {// @
            return false;
        }

        span = span.Slice(1);
        if (span.Length < KeyHelper.PublicKeyLengthInBase64 || !SignaturePublicKey.TryParse(span, out var originator))
        {// Originator
            return false;
        }

        span = span.Slice(KeyHelper.PublicKeyLengthInBase64);
        if (span.Length < 1 || span[0] != StandardSymbol)
        {// :
            return false;
        }

        instance = new Credit();
        instance.Originator = originator;
        return true;
    }

    public static int MaxStringLength
        => (1 + SignaturePublicKey.MaxStringLength) * (2 + MaxMergers);

    public int GetStringLength()
    {
        var length = 1 + this.Originator.GetStringLength() + 1 + this.Standard.GetStringLength(); // @Originator:Standard/Merger1+Merger2
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
        span[0] = StandardSymbol;
        span = span.Slice(1);

        if (!this.Standard.TryFormat(span, out w))
        {
            return false;
        }

        span = span.Slice(w);

        foreach (var x in this.mergers)
        {
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

    public Credit(SignaturePublicKey originator, SignaturePublicKey[] mergers)
    {
        this.Originator = originator;
        this.mergers = mergers;

        if (!this.Validate())
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    #region FieldAndProperty

    [Key(0)]
    public SignaturePublicKey Originator { get; private set; } = default!;

    [Key(1)]
    public SignaturePublicKey Standard { get; private set; } = default!;

    [Key(2, AddProperty = "Mergers")]
    [MaxLength(MaxMergers)]
    private SignaturePublicKey[] mergers = Array.Empty<SignaturePublicKey>();

    #endregion

    public bool Validate()
    {
        if (!this.Originator.Validate())
        {
            return false;
        }

        var keyVersion = this.Originator.KeyClass;
        if (this.Standard.KeyClass != keyVersion || !this.Standard.Validate())
        {
            return false;
        }
        else if (this.mergers == null ||
            this.mergers.Length == 0 ||
            this.mergers.Length > MaxMergers)
        {
            return false;
        }

        for (var i = 0; i < this.mergers.Length; i++)
        {
            if (this.mergers[i].KeyClass != keyVersion || !this.mergers[i].Validate())
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
        hash.Add(this.Standard);

        foreach (var x in this.mergers)
        {
            hash.Add(x);
        }

        return hash.ToHashCode();
    }
}
