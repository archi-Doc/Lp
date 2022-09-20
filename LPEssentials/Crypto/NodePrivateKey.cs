// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

[TinyhandObject]
public sealed partial class NodePrivateKey : IEquatable<NodePrivateKey>
{
    public const string Filename = "NodePrivateKey";

    public static NodePrivateKey Create()
    {
        var curve = ECCurve.CreateFromFriendlyName(NodeKey.ECCurveName);
        var ecdh = ECDiffieHellman.Create(curve);
        var key = ecdh.ExportParameters(true);

        return new NodePrivateKey(key.Q.X!, key.Q.Y!, key.D!);
    }

    public NodePrivateKey()
    {
    }

    private NodePrivateKey(byte[] x, byte[] y, byte[] d)
    {
        // this.Name = name ?? string.Empty;
        this.X = x;
        this.Y = y;
        this.D = d;
    }

    public ECDiffieHellman CreateECDH()
    {
        ECParameters p = default;
        p.Curve = NodeKey.ECCurve;
        p.D = this.d;
        return ECDiffieHellman.Create(p);
    }

    [Key(0, PropertyName = "X")]
    [MaxLength(NodeKey.PublicKeyHalfLength)]
    private byte[] x = default!;

    [Key(1, PropertyName = "Y")]
    [MaxLength(NodeKey.PublicKeyHalfLength)]
    private byte[] y = default!;

    [Key(2, PropertyName = "D")]
    [MaxLength(NodeKey.PrivateKeyLength)]
    private byte[] d = default!;

    public bool Equals(NodePrivateKey? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.x.AsSpan().SequenceEqual(other.x) &&
            this.y.AsSpan().SequenceEqual(other.y) &&
            this.d.AsSpan().SequenceEqual(other.d);
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

    public override string ToString()
        => $"{this.GetHashCode():x8}";
}
