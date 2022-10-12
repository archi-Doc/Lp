// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class NodeKey
{
    public const string PrivateKeyPath = "NodePrivateKey";

    public static PrivateKey AlternativePrivateKey
        => alternativePrivateKey ??= PrivateKey.Create(KeyType.Node);

    static NodeKey()
    {
    }

    private static PrivateKey? alternativePrivateKey;
}
