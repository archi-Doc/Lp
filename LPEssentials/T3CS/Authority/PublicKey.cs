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

    public static ECCurve ECCurve { get; }

    public static HashAlgorithmName HashAlgorithmName { get; }

    private static ObjectCache<PublicKey, ECDsa> PublicKeyToECDsa { get; } = new(100);

    static PublicKey()
    {
        ECCurve = ECCurve.CreateFromFriendlyName(ECCurveName);
        HashAlgorithmName = HashAlgorithmName.SHA256;
    }

    public PublicKey()
    {
    }

    public PublicKey(PrivateKey privateKey)
    {
        if (!privateKey.Validate())
        {
            throw new ArgumentException();
        }

        this.rawType = privateKey.RawType;
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
    private readonly byte rawType; // 6bits: KeyType, 1bit:?, 1bit: YTilde

    [Key(1)]
    private readonly ulong x0;

    [Key(2)]
    private readonly ulong x1;

    [Key(3)]
    private readonly ulong x2;

    [Key(4)]
    private readonly ulong x3;

    public uint KeyType => (uint)(this.rawType >>> 2);

    public uint YTilde => (uint)(this.rawType & 1);

    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sign)
    {
        if (sign.Length != PublicKey.SignLength)
        {
            return false;
        }

        var ecdsa = PublicKeyToECDsa.TryGet(this) ?? this.TryCreateECDsa();
        if (ecdsa == null)
        {
            return false;
        }

        var result = ecdsa.VerifyData(data, sign, HashAlgorithmName);
        PublicKeyToECDsa.Cache(this, ecdsa);
        return result;
    }

    public ECDsa? TryCreateECDsa()
    {
        if (!this.Validate())
        {
            return null;
        }

        if (this.KeyType == 0)
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

    public bool Validate()
    {
        if (this.KeyType == 0)
        {// secp256r1
            return true;
        }

        return false;
    }

    public override int GetHashCode()
        => (int)this.x0;

    public bool Equals(PublicKey other)
        => this.rawType == other.rawType &&
        this.x0 == other.x0 &&
        this.x1 == other.x1 &&
        this.x2 == other.x2 &&
        this.x3 == other.x3;

    public override string ToString()
    {
        scoped Span<byte> bytes = stackalloc byte[1 + (sizeof(ulong) * 4)];
        var b = bytes;

        b[0] = this.rawType;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, this.x0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x3);
        b = b.Slice(sizeof(ulong));

        return $"({Base64.EncodeToBase64Utf16(bytes)})";
    }
}
