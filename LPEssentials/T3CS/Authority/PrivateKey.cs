// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace LP;

#pragma warning disable SA1214

[TinyhandObject]
public sealed partial class PrivateKey : IValidatable, IEquatable<PrivateKey>
{
    public const int MaxNameLength = 16;
    private const int MaxPrivateKeyCache = 10;

    private static ObjectCache<PrivateKey, ECDsa> PrivateKeyToEcdsa { get; } = new(MaxPrivateKeyCache);

    private static ObjectCache<PrivateKey, ECDiffieHellman> PrivateKeyToEcdh { get; } = new(MaxPrivateKeyCache);

    public static PrivateKey Create(KeyType keyType)
    {
        var curve = PublicKey.KeyTypeToCurve(keyType);

        using (var ecdsa = ECDsa.Create(curve))
        {
            var key = ecdsa.ExportParameters(true);
            return new PrivateKey(keyType, key.Q.X!, key.Q.Y!, key.D!);
        }
    }

    public static PrivateKey Create(KeyType keyType, ReadOnlySpan<byte> seed)
    {
        ECParameters key = default;
        key.Curve = PublicKey.KeyTypeToCurve(keyType);

        byte[]? d = null;
        var hash = Hash.ObjectPool.Get();
        while (true)
        {
            try
            {
                if (d == null)
                {
                    d = hash.GetHash(seed);
                }
                else
                {
                    d = hash.GetHash(d);
                }

                key.D = d;
                using (var ecdsa = ECDsa.Create(key))
                {
                    key = ecdsa.ExportParameters(true);
                    break;
                }
            }
            catch
            {
            }
        }

        Hash.ObjectPool.Return(hash);
        return new PrivateKey(keyType, key.Q.X!, key.Q.Y!, key.D!);
    }

    public static PrivateKey Create(KeyType keyType, string passphrase)
    {
        ECParameters key = default;
        key.Curve = PublicKey.KeyTypeToCurve(keyType);

        var passBytes = Encoding.UTF8.GetBytes(passphrase);
        Span<byte> span = stackalloc byte[(sizeof(ulong) + passBytes.Length) * 2]; // count, passBytes, count, passBytes // scoped
        var countSpan = span.Slice(0, sizeof(ulong));
        var countSpan2 = span.Slice(sizeof(ulong) + passBytes.Length, sizeof(ulong));
        passBytes.CopyTo(span.Slice(sizeof(ulong)));
        passBytes.CopyTo(span.Slice((sizeof(ulong) * 2) + passBytes.Length));

        return Create(keyType, span);
    }

    internal PrivateKey()
    {
    }

    private PrivateKey(KeyType keyType, byte[] x, byte[] y, byte[] d)
    {
        this.x = x;
        this.y = y;
        this.d = d;

        var yTilde = this.CompressY();
        this.keyValue = (byte)((((uint)keyType << 2) & ~3) + (yTilde & 1));
    }

    public byte[]? SignData(ReadOnlySpan<byte> data)
    {
        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return null;
        }

        var sign = new byte[PublicKey.SignLength];
        if (!ecdsa.TrySignData(data, sign.AsSpan(), PublicKey.HashAlgorithmName, out var written))
        {
            return null;
        }

        this.CacheEcdsa(ecdsa);
        return sign;
    }

    public bool SignData(ReadOnlySpan<byte> data, Span<byte> signature)
    {
        if (signature.Length < PublicKey.SignLength)
        {
            return false;
        }

        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        if (!ecdsa.TrySignData(data, signature, PublicKey.HashAlgorithmName, out var written))
        {
            return false;
        }

        PrivateKeyToEcdsa.Cache(this, ecdsa);
        return true;
    }

    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sign)
    {
        var publicKey = new PublicKey(this);
        return publicKey.VerifyData(data, sign);
    }

    public byte[]? DeriveKeyMaterial(PublicKey publicKey)
    {
        if (this.KeyType != publicKey.KeyType)
        {// (uint)(this.rawType >> 2);
            return null;
        }

        var publicEcdh = publicKey.TryGetEcdh();
        if (publicEcdh == null)
        {
            return null;
        }

        var privateEcdh = this.TryGetEcdh();
        if (privateEcdh == null)
        {
            publicKey.CacheEcdh(publicEcdh);
            return null;
        }

        byte[]? material = null;
        try
        {
            material = privateEcdh.DeriveKeyMaterial(publicEcdh.PublicKey);
        }
        catch
        {
            publicKey.CacheEcdh(publicEcdh);
            return null;
        }

        this.CacheEcdh(privateEcdh);
        publicKey.CacheEcdh(publicEcdh);
        return material;
    }

    [Key(0)]
    private readonly byte keyValue; // 6bits: KeyType, 1bit:?, 1bit: YTilde

    [Key(1)]
    private readonly byte[] x = Array.Empty<byte>();

    [Key(2)]
    private readonly byte[] y = Array.Empty<byte>();

    [Key(3)]
    private readonly byte[] d = Array.Empty<byte>();

    public KeyType KeyType
    {
        get
        {
            var u = (uint)(this.keyValue >> 2);
            return Unsafe.As<uint, KeyType>(ref u);
        }
    }

    public uint YTilde => (uint)(this.keyValue & 1);

    public byte[] X => this.x;

    public byte[] Y => this.y;

    public bool Validate()
    {
        if (this.KeyType != KeyType.Authority &&
            this.KeyType != KeyType.Node)
        {
            return false;
        }
        else if (this.x == null || this.x.Length != PublicKey.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.y == null || this.y.Length != PublicKey.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.d == null || this.d.Length != PublicKey.PrivateKeyLength)
        {
            return false;
        }

        return true;
    }

    public bool Equals(PrivateKey? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.keyValue == other.keyValue &&
            this.x.AsSpan().SequenceEqual(other.x);
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(this.keyValue);

        if (this.x.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToInt32(this.x, 0);
        }

        return hash;
    }

    public override string ToString()
    {
        Span<byte> bytes = stackalloc byte[1 + PublicKey.PublicKeyHalfLength]; // scoped
        bytes[0] = this.keyValue;
        this.x.CopyTo(bytes.Slice(1));
        return $"({Base64.Url.FromByteArrayToString(bytes)})";
    }

    internal uint CompressY()
    {
        if (this.KeyType == KeyType.Authority ||
            this.KeyType == KeyType.Node)
        {
            return Arc.Crypto.EC.P256R1Curve.Instance.CompressY(this.y);
        }
        else
        {
            throw new InvalidDataException();
        }
    }

    internal byte KeyValue => this.keyValue;

    private ECDsa? TryGetEcdsa()
    {
        if (PrivateKeyToEcdsa.TryGet(this) is { } ecdsa)
        {
            return ecdsa;
        }

        if (!this.Validate())
        {
            return null;
        }

        try
        {
            ECParameters p = default;
            p.Curve = PublicKey.KeyTypeToCurve(this.KeyType);
            p.D = this.d;
            return ECDsa.Create(p);
        }
        catch
        {
        }

        return null;
    }

    private void CacheEcdsa(ECDsa ecdsa)
        => PrivateKeyToEcdsa.Cache(this, ecdsa);

    private ECDiffieHellman? TryGetEcdh()
    {
        if (PrivateKeyToEcdh.TryGet(this) is { } ecdh)
        {
            return ecdh;
        }

        if (!this.Validate())
        {
            return null;
        }

        try
        {
            ECParameters p = default;
            p.Curve = PublicKey.KeyTypeToCurve(this.KeyType);
            p.D = this.d;
            return ECDiffieHellman.Create(p);
        }
        catch
        {
        }

        return null;
    }

    private void CacheEcdh(ECDiffieHellman ecdh)
        => PrivateKeyToEcdh.Cache(this, ecdh);
}
