// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class NodeKey
{
    public const string ECCurveName = "secp256r1";
    public const int PublicKeySize = 64;
    public const int PrivateKeySize = 32;
    public const string Filename = "Node.key";

    public static ECCurve ECCurve { get; }

    static NodeKey()
    {
        ECCurve = ECCurve.CreateFromFriendlyName(ECCurveName);
    }

    public static ECDiffieHellman? FromPrivateKey(NodePrivateKey key)
    {
        try
        {
            ECParameters p = default;
            p.Curve = ECCurve;
            p.D = key.D;
            return ECDiffieHellman.Create(p);
        }
        catch
        {
            return null;
        }
    }

    public static ECDiffieHellman? FromPublicKey(byte[] x, byte[] y)
    {
        try
        {
            ECParameters p = default;
            p.Curve = ECCurve;
            p.Q.X = x;
            p.Q.Y = y;
            return ECDiffieHellman.Create(p);
        }
        catch
        {
            return null;
        }
    }
}

[TinyhandObject]
public partial class NodePrivateKey
{
    public const string ECCurveName = "secp256r1";
    public const string Filename = "NodePrivateKey";

    public static NodePrivateKey AlternativePrivateKey
    {
        get
        {
            if (alternativePrivateKey == null)
            {
                alternativePrivateKey = NodePrivateKey.Create("Alternative");
            }

            return alternativePrivateKey;
        }
    }

    public static NodePrivateKey Create(string? name = null)
    {
        var curve = ECCurve.CreateFromFriendlyName(ECCurveName);
        var ecdh = ECDiffieHellman.Create(curve);
        var key = ecdh.ExportParameters(true);

        return new NodePrivateKey(name, key.D!, key.Q.X!, key.Q.Y!);
    }

    public NodePrivateKey()
    {
    }

    public NodePrivateKey(string? name, byte[] d, byte[] x, byte[] y)
    {
        this.Name = name ?? string.Empty;
        this.D = d;
        this.X = x;
        this.Y = y;
    }

    [Key(0)]
    public string Name { get; set; } = string.Empty;

    [Key(1)]
    public byte[] D { get; set; } = default!;

    [Key(2)]
    public byte[] X { get; set; } = default!;

    [Key(3)]
    public byte[] Y { get; set; } = default!;

    private static NodePrivateKey? alternativePrivateKey;
}

[TinyhandObject]
public partial class NodePublicKey : IEquatable<NodePublicKey>
{
    public NodePublicKey()
    {
    }

    public NodePublicKey(NodePrivateKey privateKey)
    {
        this.X = privateKey.X;
        this.Y = privateKey.Y;
    }

    [Key(0)]
    public byte[] X { get; set; } = Array.Empty<byte>();

    [Key(1)]
    public byte[] Y { get; set; } = Array.Empty<byte>();

    public bool Equals(NodePublicKey? other)
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
        var x = this.X == null ? Array.Empty<byte>() : this.X.AsSpan();
        var y = this.Y == null ? Array.Empty<byte>() : this.Y.AsSpan();
        return (int)(FarmHash.Hash64(x) ^ FarmHash.Hash64(y));
    }
}

public struct NodePublicPrivateKeyStruct : IEquatable<NodePublicPrivateKeyStruct>
{
    public byte[] D;

    public byte[] X;

    public byte[] Y;

    public NodePublicPrivateKeyStruct(byte[] d, byte[] x, byte[] y)
    {
        this.D = d;
        this.X = x;
        this.Y = y;
    }

    public bool Equals(NodePublicPrivateKeyStruct other)
    {
        var d1 = this.D == null ? Array.Empty<byte>() : this.D.AsSpan();
        var d2 = other.D == null ? Array.Empty<byte>() : other.D.AsSpan();
        if (!d1.SequenceEqual(d2))
        {
            return false;
        }

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
        var d = this.D == null ? Array.Empty<byte>() : this.D.AsSpan();
        var x = this.X == null ? Array.Empty<byte>() : this.X.AsSpan();
        var y = this.Y == null ? Array.Empty<byte>() : this.Y.AsSpan();
        return (int)(FarmHash.Hash64(d) ^ FarmHash.Hash64(x) ^ FarmHash.Hash64(y));
    }
}
