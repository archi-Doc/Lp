// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Numerics;
using System.Security.Cryptography;

namespace LP;

[TinyhandObject]
public sealed partial class AuthorityPrivateKey : IValidatable, IEquatable<AuthorityPrivateKey>
{
    public const int MaxNameLength = 16;

    public static AuthorityPrivateKey Create(string? name = null)
    {
        var curve = ECCurve.CreateFromFriendlyName(Authority.ECCurveName);
        var ecdsa = ECDsa.Create(curve);
        var key = ecdsa.ExportParameters(true);

        return new AuthorityPrivateKey(name, 0, key.Q.X!, key.Q.Y!, key.D!);
    }

    public AuthorityPrivateKey()
    {
        this.identifier = Array.Empty<byte>();
    }

    private AuthorityPrivateKey(string? name, int version, byte[] x, byte[] y, byte[] d)
    {
        this.Name = name ?? string.Empty;
        this.Version = version;
        this.X = x;
        this.Y = y;
        this.D = d;

        var hash = Hash.ObjectPool.Get();
        this.identifier = hash.GetHash(TinyhandSerializer.Serialize(this));
        Hash.ObjectPool.Return(hash);
        // Identifier.FromReadOnlySpan();
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
    [MaxLength(MaxNameLength)]
    private string name = string.Empty;

    [Key(1)]
    public int Version { get; private set; }

    [Key(2, PropertyName = "X")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] x = Array.Empty<byte>();

    [Key(3, PropertyName = "Y")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] y = Array.Empty<byte>();

    [Key(4, PropertyName = "D")]
    [MaxLength(Authority.PrivateKeyLength)]
    private byte[] d = Array.Empty<byte>();

    [IgnoreMember]
    private byte[] identifier;

    public bool Validate()
    {
        if (this.name == null || this.name.Length > MaxNameLength)
        {
            return false;
        }
        else if (this.Version != 0)
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
            this.Version == other.Version &&
            this.x.AsSpan().SequenceEqual(other.x) &&
            this.y.AsSpan().SequenceEqual(other.y);
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(this.name, this.Version);

        if (this.x.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToInt32(this.x, 0);
        }

        if (this.y.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToInt32(this.y, 0);
        }

        return (int)hash;
    }

    public override string ToString()
        => $"{this.name}({Base64.EncodeToBase64Utf16(this.identifier)})";
}
