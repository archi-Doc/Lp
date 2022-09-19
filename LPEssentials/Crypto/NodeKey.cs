// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class NodeKey
{
    public const int NameLength = 16;
    public const string ECCurveName = "secp256r1";
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;

    public static ECCurve ECCurve { get; }

    public static NodePrivateKey AlternativePrivateKey
        => alternativePrivateKey ??= NodePrivateKey.Create("Alternative");

    static NodeKey()
    {
        ECCurve = ECCurve.CreateFromFriendlyName(ECCurveName);
    }

    public static byte[]? DeriveKeyMaterial(ECDiffieHellman ecdh, byte[] x, byte[] y)
    {
        if (x.Length != PublicKeyHalfLength || y.Length != PublicKeyHalfLength)
        {
            return null;
        }

        var key = new NodePublicKeyStruct(x, y);
        var publicEcdh = Cache.NodePublicKeyToECDH.TryGet(key);
        if (publicEcdh == null)
        {
            try
            {
                ECParameters p = default;
                p.Curve = NodeKey.ECCurve;
                p.Q.X = x;
                p.Q.Y = y;
                publicEcdh = ECDiffieHellman.Create(p);
            }
            catch
            {
                return null;
            }
        }

        byte[]? material = null;
        try
        {
            material = ecdh.DeriveKeyMaterial(publicEcdh.PublicKey);
        }
        catch
        {
            return null;
        }

        Cache.NodePublicKeyToECDH.Cache(key, publicEcdh);
        return material;
    }

    private static NodePrivateKey? alternativePrivateKey;
}

/*public struct NodePublicPrivateKeyStruct : IEquatable<NodePublicPrivateKeyStruct>
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
}*/
