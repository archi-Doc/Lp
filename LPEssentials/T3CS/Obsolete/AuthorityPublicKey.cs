// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP.Obsolete;

[TinyhandObject]
public sealed partial class AuthorityPublicKey : IValidatable, IEquatable<AuthorityPublicKey>
{
    /*public static AuthorityPublicKey Create(string? name = null)
    {
        var curve = ECCurve.CreateFromFriendlyName(Authority.ECCurveName);
        var ecdsa = ECDsa.Create(curve);
        var key = ecdsa.ExportParameters(true);

        return new AuthorityPublicKey(name, key.Q.X!, key.Q.Y!);
    }*/

    public AuthorityPublicKey()
    {
    }

    public AuthorityPublicKey(AuthorityPrivateKey privateKey)
    {
        this.Version = privateKey.Version;
        this.X = privateKey.X;
        this.Y = privateKey.Y;
    }

    private AuthorityPublicKey(int version, byte[] x, byte[] y)
    {
        this.Version = version;
        this.X = x;
        this.Y = y;
    }

    [Key(0)]
    public int Version { get; private set; }

    [Key(1, PropertyName = "X")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] x = Array.Empty<byte>();

    [Key(2, PropertyName = "Y")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] y = Array.Empty<byte>();

    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sign)
    {
        if (sign.Length != Authority.SignLength)
        {
            return false;
        }

        /*var ecdsa = Cache.AuthorityPublicKeyToECDsa.TryGet(this) ?? this.TryCreateECDsa();
        if (ecdsa == null)
        {
            return false;
        }

        var result = ecdsa.VerifyData(data, sign, Authority.HashAlgorithmName);
        Cache.AuthorityPublicKeyToECDsa.Cache(this, ecdsa);
        return result;*/

        return false;
    }

    public ECDsa? TryCreateECDsa()
    {
        try
        {
            ECParameters p = default;
            p.Curve = Authority.ECCurve;
            p.Q.X = this.x;
            p.Q.Y = this.y;
            return ECDsa.Create(p);
        }
        catch
        {
            return null;
        }
    }

    public bool Validate()
    {
        if (this.Version != 0)
        {
            return false;
        }
        else if (this.x == null || this.x.Length != Authority.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.y == null || this.y.Length != Authority.PublicKeyHalfLength)
        {
            return false;
        }

        return true;
    }

    public bool Equals(AuthorityPublicKey? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Version == other.Version &&
            this.x.AsSpan().SequenceEqual(other.x) &&
            this.y.AsSpan().SequenceEqual(other.y);
    }

    public override int GetHashCode()
    {
        var hash = (ulong)this.Version;

        if (this.x.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToUInt64(this.x, 0);
        }

        if (this.y.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToUInt64(this.y, 0);
        }

        return (int)hash;
    }
}

/*internal readonly struct AuthorityPublicKeyStruct : IEquatable<AuthorityPublicKeyStruct>
{
    public readonly byte[] X;

    public readonly byte[] Y;

    public AuthorityPublicKeyStruct(byte[] x, byte[] y)
    {
        this.X = x;
        this.Y = y;
    }

    public bool Equals(AuthorityPublicKeyStruct other)
    {
        var x1 = this.X == null ? Array.Empty<byte>() : this.X.AsSpan();
        var x2 = other.X == null ? Array.Empty<byte>() : other.X.AsSpan();
        if (!x1.SequenceEqual(x2))
        {
            return false;
        }

        var y1 = this.Y == null ? Array.Empty<byte>() : this.Y.AsSpan();
        var y2 = other.Y == null ? Array.Empty<byte>() : other.Y.AsSpan();
        return y1.SequenceEqual(y2);
    }

    public override int GetHashCode()
    {
        ulong hash = 0;
        if (this.X.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToUInt64(this.X, 0);
        }

        if (this.Y.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToUInt64(this.Y, 0);
        }

        return (int)hash;
    }
}*/
