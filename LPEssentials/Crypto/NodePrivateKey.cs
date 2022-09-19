// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

[TinyhandObject]
public sealed partial class NodePrivateKey : IEquatable<NodePrivateKey>
{
    public const string Filename = "NodePrivateKey";

    public static NodePrivateKey Create(string? name = null)
    {
        var curve = ECCurve.CreateFromFriendlyName(NodeKey.ECCurveName);
        var ecdh = ECDiffieHellman.Create(curve);
        var key = ecdh.ExportParameters(true);

        return new NodePrivateKey(name, key.D!, key.Q.X!, key.Q.Y!);
    }

    public NodePrivateKey()
    {
    }

    private NodePrivateKey(string? name, byte[] d, byte[] x, byte[] y)
    {
        this.Name = name ?? string.Empty;
        this.D = d;
        this.X = x;
        this.Y = y;
    }

    public ECDiffieHellman CreateECDH()
    {
        ECParameters p = default;
        p.Curve = NodeKey.ECCurve;
        p.D = this.d;
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

    [Key(0, PropertyName = "Name")]
    [MaxLength(Authority.NameLength)]
    private string name = default!;

    [Key(1, PropertyName = "D")]
    [MaxLength(NodeKey.PrivateKeyLength)]
    private byte[] d = default!;

    [Key(2, PropertyName = "X")]
    [MaxLength(NodeKey.PublicKeyHalfLength)]
    private byte[] x = default!;

    [Key(3, PropertyName = "Y")]
    [MaxLength(NodeKey.PublicKeyHalfLength)]
    private byte[] y = default!;

    public bool Equals(NodePrivateKey? other)
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
        => $"{this.name}({this.GetHashCode():x8})";
}
