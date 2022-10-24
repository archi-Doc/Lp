// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace LP;

[TinyhandObject]
public sealed partial class NodePrivateKey : IValidatable, IEquatable<NodePrivateKey>
{
    public const string PrivateKeyPath = "NodePrivateKey";

    public static NodePrivateKey AlternativePrivateKey
        => alternativePrivateKey ??= NodePrivateKey.Create();

    private const int MaxPrivateKeyCache = 10;

    private static NodePrivateKey? alternativePrivateKey;

    private static ObjectCache<NodePrivateKey, ECDiffieHellman> PrivateKeyToEcdh { get; } = new(MaxPrivateKeyCache);

    public static NodePrivateKey Create()
    {
        using (var ecdh = ECDiffieHellman.Create(NodePublicKey.ECCurve))
        {
            var key = ecdh.ExportParameters(true);
            return new NodePrivateKey(0, key.Q.X!, key.Q.Y!, key.D!);
        }
    }

    public static NodePrivateKey Create(ReadOnlySpan<byte> seed)
    {
        ECParameters key = default;
        key.Curve = NodePublicKey.ECCurve;

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

                if (!Arc.Crypto.EC.P256R1Curve.Instance.IsValidSeed(d))
                {
                    continue;
                }

                key.D = d;
                using (var ecdh = ECDiffieHellman.Create(key))
                {
                    key = ecdh.ExportParameters(true);
                    break;
                }
            }
            catch
            {
            }
        }

        Hash.ObjectPool.Return(hash);
        return new NodePrivateKey(0, key.Q.X!, key.Q.Y!, key.D!);
    }

    internal NodePrivateKey()
    {
    }

    private NodePrivateKey(uint keyVersion, byte[] x, byte[] y, byte[] d)
    {
        this.x = x;
        this.y = y;
        this.d = d;

        var yTilde = this.CompressY();
        this.keyValue = (byte)(((keyVersion << 2) & ~3) + (yTilde & 1));
    }

    public NodePublicKey ToPublicKey()
        => new(this);

    public byte[]? DeriveKeyMaterial(NodePublicKey publicKey)
    {
        if (this.KeyVersion != 0)
        {
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
    private readonly byte keyValue; // 6bits: KeyVersion, 1bit:?, 1bit: YTilde

    [Key(1)]
    private readonly byte[] x = Array.Empty<byte>();

    [Key(2)]
    private readonly byte[] y = Array.Empty<byte>();

    [Key(3)]
    private readonly byte[] d = Array.Empty<byte>();

    public uint KeyVersion => (uint)(this.keyValue >> 2);

    public uint YTilde => (uint)(this.keyValue & 1);

    public byte[] X => this.x;

    public byte[] Y => this.y;

    public bool Validate()
    {
        if (this.KeyVersion != 0)
        {
            return false;
        }
        else if (this.x == null || this.x.Length != NodePublicKey.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.y == null || this.y.Length != NodePublicKey.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.d == null || this.d.Length != NodePublicKey.PrivateKeyLength)
        {
            return false;
        }

        return true;
    }

    public bool Equals(NodePrivateKey? other)
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
        Span<byte> bytes = stackalloc byte[1 + NodePublicKey.PrivateKeyLength]; // scoped
        bytes[0] = (byte)(this.keyValue | 128); // tempcode
        this.d.CopyTo(bytes.Slice(1));
        return $"{Base64.Url.FromByteArrayToString(bytes)}";
    }

    internal uint CompressY()
    {
        if (this.KeyVersion == 0)
        {
            return Arc.Crypto.EC.P256R1Curve.Instance.CompressY(this.y);
        }
        else
        {
            return 0;
        }
    }

    internal byte KeyValue => this.keyValue;

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
            p.Curve = NodePublicKey.ECCurve;
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
