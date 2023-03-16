// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public readonly partial struct NodeKeyPair : IEquatable<NodeKeyPair>
{
    private const int MaxMaterialCache = 1_000;

    public NodeKeyPair(NodePrivateKey privateKey, NodePublicKey publicKey)
    {
        this.privateKey = privateKey;
        this.publicKey = publicKey;
    }

    public byte[]? DeriveKeyMaterial()
    {
        if (KeyPairToMaterial.TryGet(this) is { } material)
        {
            return material;
        }

        if (this.privateKey.KeyVersion != 1)
        {
            return null;
        }

        var publicEcdh = this.publicKey.TryGetEcdh();
        if (publicEcdh == null)
        {
            return null;
        }

        var privateEcdh = this.privateKey.TryGetEcdh();
        if (privateEcdh == null)
        {
            this.publicKey.CacheEcdh(publicEcdh);
            return null;
        }

        try
        {
            material = privateEcdh.DeriveKeyMaterial(publicEcdh.PublicKey);
        }
        catch
        {
            this.publicKey.CacheEcdh(publicEcdh);
            return null;
        }

        this.privateKey.CacheEcdh(privateEcdh);
        this.publicKey.CacheEcdh(publicEcdh);
        KeyPairToMaterial.Cache(this, material);
        return material;
    }

    private static ObjectCache<NodeKeyPair, byte[]> KeyPairToMaterial { get; } = new(MaxMaterialCache);

    private readonly NodePrivateKey privateKey;
    private readonly NodePublicKey publicKey;

    public bool Equals(NodeKeyPair other)
    {
        return this.privateKey.Equals(other.privateKey) &&
            this.publicKey.Equals(other.publicKey);
    }
}
