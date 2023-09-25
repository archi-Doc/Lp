// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace LP.T3CS;

/// <summary>
/// Represents a public key data. Compressed to 33 bytes (memory usage 40 bytes).
/// </summary>
[TinyhandObject]
public readonly partial struct PublicKey : IValidatable, IEquatable<PublicKey>, IPublicKey
{
    private static ObjectCache<PublicKey, ECDsa> PublicKeyToEcdsa { get; } = new(100);

    public static bool TryParse(ReadOnlySpan<char> chars, [MaybeNullWhen(false)] out PublicKey publicKey)
    {
        if (KeyHelper.TryParsePublicKey(chars, out var keyValue, out var x) &&
            KeyHelper.GetKeyClass(keyValue) == KeyClass.T3CS_Signature)
        {
            publicKey = new(keyValue, x);
            return true;
        }

        publicKey = default;
        return false;
    }

    public PublicKey()
    {
    }

    internal PublicKey(byte keyValue, ReadOnlySpan<byte> x)
    {
        this.keyValue = KeyHelper.ToPublicKeyValue(keyValue);
        var b = x;
        this.x0 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x1 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x2 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x3 = BitConverter.ToUInt64(b);
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

    public byte KeyValue => this.keyValue;

    public KeyClass KeyClass => KeyHelper.GetKeyClass(this.keyValue);

    public uint YTilde => KeyHelper.GetYTilde(this.keyValue);


    #endregion

    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sign)
    {
        if (sign.Length != KeyHelper.SignLength)
        {
            return false;
        }

        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        var result = ecdsa.VerifyData(data, sign, KeyHelper.HashAlgorithmName);
        this.CacheEcdsa(ecdsa);
        return result;
    }

    public bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> sign)
    {
        if (sign.Length != KeyHelper.SignLength)
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
        if (sign.Length != KeyHelper.SignLength)
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
        else if (this.x0 == 0 || this.x1 == 0 || this.x2 == 0 || this.x3 == 0)
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
        if (span.Length != KeyHelper.PublicKeyHalfLength)
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
        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        this.TryWriteBytes(span, out _);
        return $"{Base64.Url.FromByteArrayToString(span)}";
    }

    public Identifier ToIdentifier()
    {
        return new(this.x0, this.x1, this.x2, this.x3);
    }

    internal void WriteX(Span<byte> span)
    {
        var b = span;
        BitConverter.TryWriteBytes(b, this.x0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x3);
    }

    internal ECDiffieHellman? TryGetEcdh()
    {
        if (this.KeyClass != KeyClass.T3CS_Encryption)
        {
            return default;
        }

        var x = new byte[32];
        var span = x.AsSpan();
        BitConverter.TryWriteBytes(span, this.x0);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.x1);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.x2);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.x3);

        var y = KeyHelper.CurveInstance.TryDecompressY(x, this.YTilde);
        if (y == null)
        {
            return null;
        }

        try
        {
            ECParameters p = default;
            p.Curve = KeyHelper.ECCurve;
            p.Q.X = x;
            p.Q.Y = y;
            return ECDiffieHellman.Create(p);
        }
        catch
        {
        }

        return null;
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

        if (this.KeyClass == 0)
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

            var y = KeyHelper.CurveInstance.TryDecompressY(x, this.YTilde);
            if (y == null)
            {
                return null;
            }

            try
            {
                ECParameters p = default;
                p.Curve = KeyHelper.ECCurve;
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
