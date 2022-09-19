// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

[TinyhandObject]
public sealed partial class AuthorityPrivateKey : IValidatable, IEquatable<AuthorityPrivateKey>
{
    public static AuthorityPrivateKey Create(string? name = null)
    {
        var curve = ECCurve.CreateFromFriendlyName(Authority.ECCurveName);
        var ecdsa = ECDsa.Create(curve);
        var key = ecdsa.ExportParameters(true);

        return new AuthorityPrivateKey(name, key.D!, key.Q.X!, key.Q.Y!);
    }

    public AuthorityPrivateKey()
    {
    }

    private AuthorityPrivateKey(string? name, byte[] d, byte[] x, byte[] y)
    {
        this.Name = name ?? string.Empty;
        this.D = d;
        this.X = x;
        this.Y = y;
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

    [Key(0, PropertyName = "Name")]
    [MaxLength(Authority.NameLength)]
    private string name = default!;

    [Key(1, PropertyName = "D")]
    [MaxLength(Authority.PrivateKeyLength)]
    private byte[] d = default!;

    [Key(2, PropertyName = "X")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] x = default!;

    [Key(3, PropertyName = "Y")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] y = default!;

    public bool Validate()
    {
        if (this.name == null || this.name.Length > Authority.NameLength)
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

        return this.name.Equals(other.name) &&
            this.x.AsSpan().SequenceEqual(other.x) &&
            this.y.AsSpan().SequenceEqual(other.y) &&
            this.d.AsSpan().SequenceEqual(other.d);
    }

    public override int GetHashCode()
    {
        var hash = FarmHash.Hash64(this.name);

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
        => $"{this.name}"; // {this.GetHashCode():x8}
}
