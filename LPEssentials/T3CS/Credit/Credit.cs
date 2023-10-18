// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LP.T3CS;

/// <summary>
/// Represents a credit information (@Originator:Standard/Merger1+Merger2).
/// </summary>
[TinyhandObject]
public sealed partial class Credit : IValidatable, IEquatable<Credit>
{
    public const char CreditSymbol = '@';
    public const char StandardSymbol = ':';
    public const char MergerSymbol = '/';
    public const char MergerSeparator = '+';
    public const int MaxMergers = 4;
    public static readonly Credit Default = new();

    public static bool TryParse(ReadOnlySpan<char> chars, [MaybeNullWhen(false)] out Credit instance)
    {
        instance = default;
        var span = chars.Trim();

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

    public string ToBase64()
    {
        var sb = new StringBuilder();

        sb.Append(CreditSymbol);
        sb.Append(this.Originator.ToBase64());
        sb.Append(StandardSymbol);
        sb.Append(this.Standard.ToBase64());

        if (this.mergers.Length == 0)
        {
            return sb.ToString();
        }
        else
        {
            sb.Append(MergerSymbol);
            for (var i = 0; i < this.mergers.Length; i++, sb.Append(MergerSeparator))
            {
                sb.Append(this.mergers[i].ToBase64());
            }

            return sb.ToString();
        }
    }
}
