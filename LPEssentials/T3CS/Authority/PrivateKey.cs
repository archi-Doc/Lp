// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LP;

#pragma warning disable SA1214

[TinyhandObject]
public sealed partial class PrivateKey : IValidatable, IEquatable<PrivateKey>
{
    public const int MaxNameLength = 16;

    private static ObjectCache<PrivateKey, ECDsa> PrivateKeyToECDsa { get; } = new(10);

    public static PrivateKey Create(string name)
    {
        using (var ecdsa = ECDsa.Create(PublicKey.ECCurve))
        {
            var key = ecdsa.ExportParameters(true);
            return new PrivateKey(name, 0, key.Q.X!, key.Q.Y!, key.D!);
        }
    }

    public static PrivateKey Create(string name, string passphrase)
    {
        ECParameters key = default;
        key.Curve = PublicKey.ECCurve;

        var passBytes = Encoding.UTF8.GetBytes(passphrase);
        scoped Span<byte> span = stackalloc byte[(sizeof(ulong) + passBytes.Length) * 2]; // count, passBytes, count, passBytes
        var countSpan = span.Slice(0, sizeof(ulong));
        var countSpan2 = span.Slice(sizeof(ulong) + passBytes.Length, sizeof(ulong));
        passBytes.CopyTo(span.Slice(sizeof(ulong)));
        passBytes.CopyTo(span.Slice((sizeof(ulong) * 2) + passBytes.Length));

        var hash = Hash.ObjectPool.Get();
        ulong count = 0;
        while (true)
        {
            BitConverter.TryWriteBytes(countSpan, count);
            BitConverter.TryWriteBytes(countSpan2, count);
            count++;

            try
            {
                var d = hash.GetHash(span);
                key.D = d;
                using (var ecdsa = ECDsa.Create(key))
                {
                    key = ecdsa.ExportParameters(true); // !d.SequenceEqual(key.D)
                    break;
                }
            }
            catch
            {
            }
        }

        Hash.ObjectPool.Return(hash);
        return new PrivateKey(name, 0, key.Q.X!, key.Q.Y!, key.D!);
    }

    public PrivateKey()
    {
    }

    private PrivateKey(string? name, byte keyType, byte[] x, byte[] y, byte[] d)
    {
        this.Name = name ?? string.Empty;
        this.keyType = keyType;
        this.x = x;
        this.y = y;
        this.d = d;

        /*var hash = Hash.ObjectPool.Get();
        this.identifier = hash.GetHash(TinyhandSerializer.Serialize(this));
        Hash.ObjectPool.Return(hash);*/
        // Identifier.FromReadOnlySpan();
    }

    public byte[]? SignData(ReadOnlySpan<byte> data)
    {
        var ecdsa = PrivateKeyToECDsa.TryGet(this) ?? this.TryCreateECDsa();
        if (ecdsa == null)
        {
            return null;
        }

        var sign = new byte[PublicKey.SignLength];
        if (!ecdsa.TrySignData(data, sign.AsSpan(), PublicKey.HashAlgorithmName, out var written))
        {
            return null;
        }

        PrivateKeyToECDsa.Cache(this, ecdsa);
        return sign;
    }

    public ECDsa? TryCreateECDsa()
    {
        if (!this.Validate())
        {
            return null;
        }

        if (this.KeyType == 0)
        {
            try
            {
                ECParameters p = default;
                p.Curve = PublicKey.ECCurve;
                p.D = this.d;
                return ECDsa.Create(p);
            }
            catch
            {
            }
        }

        return null;
    }

    [Key(0, PropertyName = "Name")]
    [MaxLength(MaxNameLength)]
    private string name = string.Empty;

    [Key(1)]
    private readonly byte keyType;

    [Key(2)]
    private readonly byte[] x = Array.Empty<byte>();

    [Key(3)]
    private readonly byte[] y = Array.Empty<byte>();

    [Key(4)]
    private readonly byte[] d = Array.Empty<byte>();

    public uint KeyType => (uint)(this.keyType & ~1);

    public byte[] X => this.x;

    /*[IgnoreMember]
    private byte[] identifier;*/

    public bool Validate()
    {
        if (this.name == null || this.name.Length > MaxNameLength)
        {
            return false;
        }
        else if (this.keyType != 0)
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

        return this.name.Equals(other.name) &&
            this.keyType == other.keyType &&
            this.x.AsSpan().SequenceEqual(other.x);
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(this.name, this.keyType);

        if (this.x.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToInt32(this.x, 0);
        }

        return hash;
    }

    public override string ToString()
        => $"{this.name}({Base64.EncodeToBase64Utf16(this.x)})";

    internal uint CompressY()
        => Arc.Crypto.EC.P256R1Curve.Instance.CompressY(this.y);
}
