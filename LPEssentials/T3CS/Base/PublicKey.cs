// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Arc.Crypto.EC;

namespace LP.T3CS;

/// <summary>
/// Represents a public key data. Compressed to 33 bytes.
/// </summary>
[TinyhandObject]
public readonly partial struct PublicKey : IValidatable, IEquatable<PublicKey>
{
    // public const string ECCurveName = "secp256r1"; // CurveInstance
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;
    public const int SignLength = 64;
    public const int EncodedLength = 1 + (sizeof(ulong) * 4);
    public const int MaxPublicKeyCache = 100;

    public static readonly HashAlgorithmName HashAlgorithmName;
    public static readonly ECCurveBase CurveInstance;

    internal static ECCurve ECCurve { get; }

    private static ObjectCache<PublicKey, ECDsa> PublicKeyToEcdsa { get; } = new(MaxPublicKeyCache);

    static PublicKey()
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
        this.keyValue = 0;
        this.x0 = 0;
        this.x1 = 0;
        this.x2 = 0;
        this.x3 = 0;
    }

    /*public PublicKey(string base64url)
    {
        var bytes = Arc.Crypto.Base64.Url.FromStringToByteArray(base64url);
        if (bytes.Length == (PublicKeyHalfLength + 1))
        {
            var span = bytes.AsSpan();
            this.keyValue = span[0];
            span = span.Slice(1);
            this.x0 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.x1 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.x2 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.x3 = BitConverter.ToUInt64(span);
        }
        else
        {
            this.keyValue = 0;
            this.x0 = 0;
            this.x1 = 0;
            this.x2 = 0;
            this.x3 = 0;
        }
    }*/

    public LinkageKey ToLinkageKey()
        => new(this);

    public LinkageKey ToLinkageKey(NodePublicKey encryptionKey)
        => new(this, encryptionKey);

    internal PublicKey(PrivateKey privateKey)
    {
        this.keyValue = KeyHelper.CheckPublicKeyValue(privateKey.KeyValue);
        var span = privateKey.X.AsSpan();
        this.x0 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.x1 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.x2 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.x3 = BitConverter.ToUInt64(span);
    }

    private PublicKey(byte keyValue, ulong x0, ulong x1, ulong x2, ulong x3)
    {
        this.keyValue = KeyHelper.CheckPublicKeyValue(keyValue);
        this.x0 = x0;
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
    }

    #region FieldAndProperty

    [Key(0)]
    private readonly byte keyValue;

    [Key(1)]
    private readonly ulong x0;

    [Key(2)]
    private readonly ulong x1;

    [Key(3)]
    private readonly ulong x2;

    [Key(4)]
    private readonly ulong x3;

    public uint KeyVersion => KeyHelper.ToKeyVersion(this.keyValue);

    public uint YTilde => KeyHelper.ToYTilde(this.keyValue);

    internal byte KeyValue => this.keyValue;

    #endregion

    public unsafe ulong GetChecksum()
    {
        Span<byte> span = stackalloc byte[EncodedLength];
        this.TryWriteBytes(span, out _);
        return FarmHash.Hash64(span);

        // FarmHash.Hash64(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(this)), sizeof(PublicKey)));

        /*var writer = default(TinyhandWriter);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this);
            return FarmHash.Hash64(writer.FlushAndGetReadOnlySpan());
        }
        finally
        {
            writer.Dispose();
        }*/
    }

    public bool IsValid() =>
        this.x0 != 0 &&
        this.x1 != 0 &&
        this.x2 != 0 &&
        this.x3 != 0;

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
        if (this.KeyVersion == 0)
        {
            return true;
        }

        return false;
    }

    public bool IsSameKey(PrivateKey privateKey)
    {
        var span = privateKey.X.AsSpan();
        if (span.Length != PublicKeyHalfLength)
        {
            return false;
        }

        if (this.x0 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.x1 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.x2 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.x3 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        if (this.YTilde != privateKey.YTilde)
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
        => (int)this.x0;

    public bool Equals(PublicKey other)
        => this.keyValue == other.keyValue &&
        this.x0 == other.x0 &&
        this.x1 == other.x1 &&
        this.x2 == other.x2 &&
        this.x3 == other.x3;

    public override string ToString()
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
        return new(this.x0, this.x1, this.x2, this.x3);
    }

    public bool TryWriteBytes(Span<byte> span, out int written)
    {
        if (span.Length < EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        b[0] = this.keyValue;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, this.x0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x3);
        b = b.Slice(sizeof(ulong));

        written = EncodedLength;
        return true;
    }

    private ECDsa? TryGetEcdsa()
    {
        if (PublicKeyToEcdsa.TryGet(this) is { } ecdsa)
        {
            return ecdsa;
        }

        if (!this.Validate())
        {
            return null;
        }

        if (this.KeyVersion == 0)
        {
            var x = new byte[32];
            var span = x.AsSpan();
            BitConverter.TryWriteBytes(span, this.x0);
            span = span.Slice(sizeof(ulong));
            BitConverter.TryWriteBytes(span, this.x1);
            span = span.Slice(sizeof(ulong));
            BitConverter.TryWriteBytes(span, this.x2);
            span = span.Slice(sizeof(ulong));
            BitConverter.TryWriteBytes(span, this.x3);

            var y = Arc.Crypto.EC.P256R1Curve.Instance.TryDecompressY(x, this.YTilde);
            if (y == null)
            {
                return null;
            }

            try
            {
                ECParameters p = default;
                p.Curve = ECCurve;
                p.Q.X = x;
                p.Q.Y = y;
                return ECDsa.Create(p);
            }
            catch
            {
            }
        }

        return null;
    }

    private void CacheEcdsa(ECDsa ecdsa)
        => PublicKeyToEcdsa.Cache(this, ecdsa);
}
