// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using static LP.ByteArrayPool;

namespace LP;

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
        this.Name = privateKey.Name;
        this.X = privateKey.X;
        this.Y = privateKey.Y;
    }

    private AuthorityPublicKey(string? name, byte[] x, byte[] y)
    {
        this.Name = name ?? string.Empty;
        this.X = x;
        this.Y = y;
    }

    [Key(0, PropertyName = "Name")]
    [MaxLength(Authority.NameLength)]
    private string name = default!;

    [Key(1, PropertyName = "X")]
    [MaxLength(Authority.PublicKeyHalfLength)]
    private byte[] x = default!;

    [Key(2, PropertyName = "Y")]
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

        return true;
    }

    public bool Equals(AuthorityPublicKey? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.name.Equals(other.name) &&
            this.x.AsSpan().SequenceEqual(other.x) &&
            this.y.AsSpan().SequenceEqual(other.y);
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
}
