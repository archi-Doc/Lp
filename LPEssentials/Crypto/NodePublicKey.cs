// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

[TinyhandObject]
public partial class NodePublicKey : IEquatable<NodePublicKey>
{
    public NodePublicKey()
    {
    }

    public NodePublicKey(NodePrivateKey privateKey)
    {
        this.Name = privateKey.Name;
        this.X = privateKey.X;
        this.Y = privateKey.Y;
    }

    [Key(0, PropertyName = "Name")]
    [MaxLength(NodeKey.NameLength)]
    private string name = string.Empty;

    [Key(2, PropertyName = "X")]
    [MaxLength(NodeKey.PublicKeyHalfLength)]
    private byte[] x = Array.Empty<byte>();

    [Key(3, PropertyName = "Y")]
    [MaxLength(NodeKey.PublicKeyHalfLength)]
    private byte[] y = Array.Empty<byte>();

    public ECDiffieHellman CreateECDH()
    {
        ECParameters p = default;
        p.Curve = NodeKey.ECCurve;
        p.Q.X = this.x;
        p.Q.Y = this.y;
        return ECDiffieHellman.Create(p);
    }

    public ECDiffieHellman? TryCreateECDH()
    {
        try
        {
            return this.CreateECDH();
        }
        catch
        {
            return null;
        }
    }

    public bool Equals(NodePublicKey? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.name.Equals(other.name) &&
            this.X.AsSpan().SequenceEqual(other.X) &&
            this.Y.AsSpan().SequenceEqual(other.Y);
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

internal readonly struct NodePublicKeyStruct : IEquatable<NodePublicKeyStruct>
{
    public readonly byte[] X;

    public readonly byte[] Y;

    public NodePublicKeyStruct(byte[] x, byte[] y)
    {
        this.X = x;
        this.Y = y;
    }

    public bool Equals(NodePublicKeyStruct other)
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
