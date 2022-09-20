// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class Cache
{
    internal static ObjectCache<NodePublicKeyStruct, ECDiffieHellman> NodePublicKeyToECDH { get; } = new(100);

    // public static ObjectCache<NodePublicPrivateKeyStruct, byte[]> NodePublicPrivateKeyToMaterial { get; } = new(100);

    internal static ObjectCache<AuthorityPublicKey, ECDsa> AuthorityPublicKeyToECDsa { get; } = new(100);
}
