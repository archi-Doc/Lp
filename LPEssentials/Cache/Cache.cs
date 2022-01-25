// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class Cache
{
    public static ObjectCache<NodePublicKey, ECDiffieHellman> NodePublicKeyToECDH = new(100);
}
