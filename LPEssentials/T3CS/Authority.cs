// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

/// <summary>
/// Immutable authority (name + public key).
/// </summary>
[TinyhandObject]
public partial class Authority : IValidatable // , IEquatable<Authority>, IComparable<Authority>
{
    public const int NameLength = 16;
    public const string ECCurveName = "secp256r1";
    public const int PublicKeyLength = 64;
    public const int PrivateKeyLength = 32;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;

    public static ECCurve ECCurve { get; }

    static Authority()
    {
        ECCurve = ECCurve.CreateFromFriendlyName(ECCurveName);
    }

    public static ECDsa? FromPrivateKey(AuthorityPrivateKey key)
    {
        try
        {
            ECParameters p = default;
            p.Curve = ECCurve;
            p.D = key.D;
            return ECDsa.Create(p);
        }
        catch
        {
            return null;
        }
    }

    public static ECDsa? FromPublicKey(byte[] x, byte[] y)
    {
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
            return null;
        }
    }

    public Authority()
    {
        this.Name = string.Empty;
        this.X = default!;
        this.Y = default!;
    }

    public Authority(string name)
    {
        this.Name = name;
        this.X = default!;
        this.Y = default!;
    }

    [Key(0)]
    public string Name { get; private set; }

    [Key(1)]
    public byte[] X { get; private set; }

    [Key(2)]
    public byte[] Y { get; private set; }

    public bool Validate()
    {
        if (this.Name == null || this.Name.Length > NameLength)
        {
            return false;
        }
        else if (this.X == null || this.X.Length != PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.Y == null || this.Y.Length != PublicKeyHalfLength)
        {
            return false;
        }

        return true;
    }
}

[TinyhandObject]
public partial class AuthorityPrivateKey
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

    public AuthorityPrivateKey(string? name, byte[] d, byte[] x, byte[] y)
    {
        if (name != null && name.Length > Authority.NameLength)
        {
            name = name.Substring(0, Authority.NameLength);
        }

        this.Name = name ?? string.Empty;
        this.D = d;
        this.X = x;
        this.Y = y;
    }

    [Key(0)]
    public string Name { get; set; } = default!;

    [Key(1)]
    public byte[] D { get; set; } = default!;

    [Key(2)]
    public byte[] X { get; set; } = default!;

    [Key(3)]
    public byte[] Y { get; set; } = default!;
}

/*[TinyhandObject]
public partial class AuthorityPublicKey : IEquatable<AuthorityPublicKey>
{
    public AuthorityPublicKey()
    {
    }

    public AuthorityPublicKey(AuthorityPrivateKey privateKey)
    {
        this.X = privateKey.X;
        this.Y = privateKey.Y;
    }

    [Key(0)]
    public byte[] X { get; set; } = Array.Empty<byte>();

    [Key(1)]
    public byte[] Y { get; set; } = Array.Empty<byte>();

    public bool Equals(AuthorityPublicKey? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.X.AsSpan().SequenceEqual(other.X) && this.Y.AsSpan().SequenceEqual(other.Y);
    }

    public override int GetHashCode()
    {
        return (int)(FarmHash.Hash64(this.X) ^ FarmHash.Hash64(this.Y));
    }
}*/
