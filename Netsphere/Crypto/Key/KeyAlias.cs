// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Netsphere.Crypto;

public static class KeyAlias
{// PublicKey <-> Alias
    private static readonly Lock LockSignaturePublicKey = new();
    private static readonly NotThreadsafeHashtable<SignaturePublicKey2, string> SignaturePublicKeyToAlias = new();
    private static readonly NotThreadsafeHashtable<string, SignaturePublicKey2> AliasToSignaturePublicKey = new();

    public static void AddAlias(SignaturePublicKey2 publicKey, string alias)
    {
        using (LockSignaturePublicKey.EnterScope())
        {
            SignaturePublicKeyToAlias.Add(publicKey, alias);
            AliasToSignaturePublicKey.Add(alias, publicKey);
        }
    }

    public static bool TryGetAlias(SignaturePublicKey2 publicKey, [MaybeNullWhen(false)] out string alias)
        => SignaturePublicKeyToAlias.TryGetValue(publicKey, out alias);

    public static bool TryGetAlias(string alias, out SignaturePublicKey2 publicKey)
        => AliasToSignaturePublicKey.TryGetValue(alias, out publicKey);
}
