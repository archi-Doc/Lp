// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

#pragma warning disable SA1401 // Fields should be private

namespace LP.Obsolete;

[TinyhandObject]
internal partial class AuthorityKeyBase : IValidatable, IEquatable<AuthorityKeyBase>
{
    public AuthorityKeyBase()
    {
    }

    private AuthorityKeyBase(byte[] x, byte[] y)
    {
        this.X = x;
        this.Y = y;
    }

    [Key(0, PropertyName = "X")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    protected byte[] x = Array.Empty<byte>();

    [Key(1, PropertyName = "Y")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    protected byte[] y = Array.Empty<byte>();

    /*public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sign)
    {
        if (sign.Length != Authority.SignLength)
        {
            return false;
        }

        var key = new PublicKeyXY(this.x, this.y);
        var ecdsa = Cache.AuthorityPublicKeyToECDsa.TryGet(key) ?? this.TryCreateECDsa();
        if (ecdsa == null)
        {
            return false;
        }

        var result = ecdsa.VerifyData(data, sign, Authority.HashAlgorithmName);
        Cache.AuthorityPublicKeyToECDsa.Cache(key, ecdsa);
        return result;
    }*/

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
        if (this.x == null || this.x.Length != Authority.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.y == null || this.y.Length != Authority.PublicKeyHalfLength)
        {
            return false;
        }

        return true;
    }

    public bool Equals(AuthorityKeyBase? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.x.AsSpan().SequenceEqual(other.x) &&
            this.y.AsSpan().SequenceEqual(other.y);
    }

    public override int GetHashCode()
    {
        ulong hash = 0;
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

internal readonly struct PublicKeyXY : IEquatable<PublicKeyXY>
{
    public readonly byte[] X;

    public readonly byte[] Y;

    public PublicKeyXY(byte[] x, byte[] y)
    {
        this.X = x;
        this.Y = y;
    }

    public bool Equals(PublicKeyXY other)
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
}
