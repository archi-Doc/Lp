// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using LP.T3CS;

namespace LP;

public readonly partial record struct NodeKeyPair
{
    private const int MaxCache = 1_000;

    public NodeKeyPair(NodePrivateKey privateKey, NodePublicKey publicKey)
    {
        this.privateKey = privateKey;
        this.publicKey = publicKey;
    }

    public byte[]? DeriveKeyMaterial()
    {
        if (Cache.TryGet(this) is { } material)
        {
            Cache.Cache(this, material);
            return material;
        }

        if (this.privateKey.KeyClass != KeyClass.Node_Encryption ||
            this.publicKey.KeyClass != KeyClass.Node_Encryption)
        {
            return null;
        }

        using (var cache = this.publicKey.TryGetEcdh())
        {
            if (cache.Object == null)
            {
                return null;
            }

            var privateEcdh = this.privateKey.TryGetEcdh();
            if (privateEcdh == null)
            {
                return null;
            }

            try
            {
                material = privateEcdh.DeriveKeyMaterial(cache.Object.PublicKey);
            }
            catch
            {
                return null;
            }

            Cache.Cache(this, material);
            return material;
        }
    }

    private static ObjectCache<NodeKeyPair, byte[]> Cache { get; } = new(MaxCache);

    private readonly NodePrivateKey privateKey;
    private readonly NodePublicKey publicKey;
}
