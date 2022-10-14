// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

[TinyhandObject]
public readonly partial struct PublicKey : IValidatable, IEquatable<PublicKey>
{
    public const string ECCurveName = "secp256r1";
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;
    public const int SignLength = 64;
    private const int MaxPublicKeyCache = 100;

    public static HashAlgorithmName HashAlgorithmName { get; }

    internal static ECCurve ECCurve { get; }

    private static ObjectCache<PublicKey, ECDsa> PublicKeyToEcdsa { get; } = new(MaxPublicKeyCache);

    static PublicKey()
    {
        ECCurve = ECCurve.CreateFromFriendlyName(ECCurveName);
        HashAlgorithmName = HashAlgorithmName.SHA256;
    }

    public PublicKey()
    {
        this.keyValue = 0;
        this.x0 = 0;
        this.x1 = 0;
        this.x2 = 0;
        this.x3 = 0;
    }

    internal PublicKey(PrivateKey privateKey)
    {
        this.keyValue = privateKey.KeyValue;
        var span = privateKey.X.AsSpan();
        this.x0 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.x1 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.x2 = BitConverter.ToUInt64(span);
        span = span.Slice(sizeof(ulong));
        this.x3 = BitConverter.ToUInt64(span);
    }

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

    public uint KeyVersion => (uint)(this.keyValue >> 2);

    public uint YTilde => (uint)(this.keyValue & 1);

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
        Span<byte> bytes = stackalloc byte[1 + (sizeof(ulong) * 4)]; // scoped
        var b = bytes;

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

        return $"({Base64.Url.FromByteArrayToString(bytes)})";
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
