// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

[TinyhandObject]
public sealed partial class AuthorityPrivateKey : IValidatable, IEquatable<AuthorityPrivateKey>
{
    public static AuthorityPrivateKey Create()
    {
        var curve = ECCurve.CreateFromFriendlyName(Authority.ECCurveName);
        var ecdsa = ECDsa.Create(curve);
        var key = ecdsa.ExportParameters(true);

        return new AuthorityPrivateKey(0, key.Q.X!, key.Q.Y!, key.D!);
    }

    public AuthorityPrivateKey()
    {
    }

    private AuthorityPrivateKey(int version, byte[] x, byte[] y, byte[] d)
    {
        this.Version = version;
        this.X = x;
        this.Y = y;
        this.D = d;
    }

    public ECDsa? TryCreateECDsa()
    {
        try
        {
            ECParameters p = default;
            p.Curve = Authority.ECCurve;
            p.D = this.d;
            return ECDsa.Create(p);
        }
        catch
        {
            return null;
        }
    }

    [Key(0)]
    public int Version { get; private set; }

    [Key(1, PropertyName = "X")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] x = Array.Empty<byte>();

    [Key(2, PropertyName = "Y")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] y = Array.Empty<byte>();

    [Key(3, PropertyName = "D")]
    [MaxLength(Authority.PrivateKeyLength)]
    private byte[] d = Array.Empty<byte>();

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
        else if (this.d == null || this.d.Length != Authority.PrivateKeyLength)
        {
            return false;
        }

        return true;
    }

    public bool Equals(AuthorityPrivateKey? other)
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

    public override string ToString()
        => $"{this.GetHashCode():x8}";
}
