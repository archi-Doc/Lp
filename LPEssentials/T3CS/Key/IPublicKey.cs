// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

public interface IPublicKey
{
    byte KeyValue { get; }

    ulong X0 { get; }

    ulong X1 { get; }

    ulong X2 { get; }

    ulong X3 { get; }
}

/*
/// <summary>
/// Represents a public key data. Compressed to 33 bytes (memory usage 40 bytes).
/// </summary>
[TinyhandObject]
public interface IPublicKey : IValidatable, IEquatable<IPublicKey>
{
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;
    public const int SignLength = 64;
    public const int EncodedLength = 1 + (sizeof(ulong) * 4);
    public const int MaxPublicKeyCache = 100;

    public static readonly HashAlgorithmName HashAlgorithmName;
    public static readonly ECCurveBase CurveInstance;

    internal static ECCurve ECCurve { get; }

    static IPublicKey()
    {
        CurveInstance = P256R1Curve.Instance;
        ECCurve = ECCurve.CreateFromFriendlyName(CurveInstance.CurveName);
        HashAlgorithmName = HashAlgorithmName.SHA256;
    }

    public static bool TryParse(ReadOnlySpan<char> chars, [MaybeNullWhen(false)] out PublicKey publicKey)
    {
        if (chars.Length >= 2 && chars[0] == '(' && chars[chars.Length - 1] == ')')
        {
            chars = chars.Slice(1, chars.Length - 2);
        }

        publicKey = default;
        var bytes = Base64.Url.FromStringToByteArray(chars);
        if (bytes.Length != EncodedLength)
        {
            return false;
        }

        var b = bytes.AsSpan();
        var keyValue = b[0];
        b = b.Slice(1);
        var x0 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        var x1 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        var x2 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        var x3 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));

        publicKey = new(keyValue, x0, x1, x2, x3);
        return true;
    }

    public PublicKey()
    {
        this.KeyValue = 0;
        this.X0 = 0;
        this.X1 = 0;
        this.X2 = 0;
        this.X3 = 0;
    }

    internal PublicKey(PrivateKey privateKey)
    {
        this.KeyValue = KeyHelper.ToPublicKeyValue(privateKey.KeyValue);
        var span = privateKey.X.AsSpan();
        this.X0 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.X1 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.X2 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.X3 = BitConverter.ToUInt64(span);
    }

    internal PublicKey(byte keyValue, ReadOnlySpan<byte> x)
    {
        this.KeyValue = KeyHelper.ToPublicKeyValue(keyValue);
        var b = x;
        this.X0 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.X1 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.X2 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.X3 = BitConverter.ToUInt64(b);
    }

    private PublicKey(byte keyValue, ulong X0, ulong X1, ulong X2, ulong X3)
    {
        this.KeyValue = KeyHelper.ToPublicKeyValue(keyValue);
        this.X0 = X0;
        this.X1 = X1;
        this.X2 = X2;
        this.X3 = X3;
    }

    #region FieldAndProperty

    protected byte KeyValue { get; set; }

    protected ulong X0 { get; set; }

    protected ulong X1 { get; set; }

    protected ulong X2 { get; set; }

    protected ulong X3 { get; set; }

    public KeyClass KeyClass => KeyHelper.GetKeyClass(this.KeyValue);

    public uint YTilde => KeyHelper.GetYTilde(this.KeyValue);

    #endregion

    public unsafe ulong GetChecksum()
    {
        Span<byte> span = stackalloc byte[EncodedLength];
        this.TryWriteBytes(span, out _);
        return FarmHash.Hash64(span);
    }

    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sign)
    {
        if (sign.Length != PublicKey.SignLength)
        {
            return false;
        }

        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        var result = ecdsa.VerifyData(data, sign, HashAlgorithmName);
        this.CacheEcdsa(ecdsa);
        return result;
    }

    public bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> sign)
    {
        if (sign.Length != PublicKey.SignLength)
        {
            return false;
        }

        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        var result = ecdsa.VerifyHash(hash, sign);
        this.CacheEcdsa(ecdsa);
        return result;
    }

    public unsafe bool VerifyIdentifier(Identifier identifier, ReadOnlySpan<byte> sign)
    {
        if (sign.Length != PublicKey.SignLength)
        {
            return false;
        }

        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        var result = ecdsa.VerifyHash(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref identifier), sizeof(Identifier)), sign);
        this.CacheEcdsa(ecdsa);
        return result;
    }

    public bool Validate()
    {
        if (this.KeyClass > KeyHelper.UpperKeyClass)
        {
            return false;
        }
        else if (this.X0 == 0 || this.X1 == 0 || this.X2 == 0 || this.X3 == 0)
        {
            return false;
        }

        return true;
    }

    public bool Validate(KeyClass keyClass)
    {
        if (this.KeyClass != keyClass)
        {
            return false;
        }

        return this.Validate();
    }

    public bool IsSameKey(PrivateKey privateKey)
    {
        var span = privateKey.X.AsSpan();
        if (span.Length != PublicKeyHalfLength)
        {
            return false;
        }

        if (this.X0 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.X1 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.X2 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.X3 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        if (this.YTilde != privateKey.YTilde)
        {
            return false;
        }

        return true;
    }

    public bool Equals(IPublicKey other)
        => this.KeyValue == other.KeyValue &&
        this.X0 == other.X0 &&
        this.X1 == other.X1 &&
        this.X2 == other.X2 &&
        this.X3 == other.X3;

    public string ToString()
    {
        return $"({this.ToBase64()})";
    }

    public string ToBase64()
    {
        Span<byte> span = stackalloc byte[EncodedLength];
        this.TryWriteBytes(span, out _);
        return $"{Base64.Url.FromByteArrayToString(span)}";
    }

    public Identifier ToIdentifier()
    {
        return new(this.X0, this.X1, this.X2, this.X3);
    }

    public bool TryWriteBytes(Span<byte> span, out int written)
    {
        if (span.Length < EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        b[0] = this.KeyValue;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, this.X0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.X1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.X2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.X3);
        b = b.Slice(sizeof(ulong));

        written = EncodedLength;
        return true;
    }

    internal void WriteX(Span<byte> span)
    {
        var b = span;
        BitConverter.TryWriteBytes(b, this.X0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.X1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.X2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.X3);
    }
}
*/
