// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace LP;

[TinyhandObject]
public readonly partial struct NodePublicKey : IValidatable, IEquatable<NodePublicKey>
{
    public const string ECCurveName = "secp256r1";
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;
    public const int SignLength = 64;
    public const int PublicKeyEncodedLength = 1 + (sizeof(ulong) * 4);
    private const int MaxPublicKeyCache = 100;

    public static HashAlgorithmName HashAlgorithmName { get; }

    internal static ECCurve ECCurve { get; }

    private static ObjectCache<NodePublicKey, ECDiffieHellman> PublicKeyToEcdh { get; } = new(MaxPublicKeyCache);

    static NodePublicKey()
    {
        ECCurve = ECCurve.CreateFromFriendlyName(ECCurveName);
        HashAlgorithmName = HashAlgorithmName.SHA256;
    }

    public static bool TryParse(ReadOnlySpan<char> chars, [MaybeNullWhen(false)] out NodePublicKey publicKey)
    {
        if (chars.Length >= 2 && chars[0] == '(' && chars[chars.Length - 1] == ')')
        {
            chars = chars.Slice(1, chars.Length - 2);
        }

        publicKey = default;
        var bytes = Base64.Url.FromStringToByteArray(chars);
        if (bytes.Length != PublicKeyEncodedLength)
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

    public NodePublicKey()
    {
        this.keyValue = 0;
        this.x0 = 0;
        this.x1 = 0;
        this.x2 = 0;
        this.x3 = 0;
    }

    internal NodePublicKey(NodePrivateKey privateKey)
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

    private NodePublicKey(byte keyValue, ulong x0, ulong x1, ulong x2, ulong x3)
    {
        this.keyValue = KeyHelper.CheckPublicKeyValue(keyValue);
        this.x0 = x0;
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
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

    public uint KeyVersion => KeyHelper.ToKeyVersion(this.keyValue);

    public uint YTilde => KeyHelper.ToYTilde(this.keyValue);

    public bool Validate()
    {
        if (this.KeyVersion == 1)
        {
            return true;
        }

        return false;
    }

    public override int GetHashCode()
        => (int)this.x0;

    public bool Equals(NodePublicKey other)
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

        return Base64.Url.FromByteArrayToString(bytes);
    }

    internal ECDiffieHellman? TryGetEcdh()
    {
        if (PublicKeyToEcdh.TryGet(this) is { } ecdh)
        {
            return ecdh;
        }

        if (!this.Validate())
        {
            return null;
        }

        if (this.KeyVersion == 1)
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
                return ECDiffieHellman.Create(p);
            }
            catch
            {
            }
        }

        return null;
    }

    internal void CacheEcdh(ECDiffieHellman ecdh)
        => PublicKeyToEcdh.Cache(this, ecdh);
}
