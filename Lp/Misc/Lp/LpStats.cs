// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LpStats
{
    public const string Filename = "LpStats.tinyhand";

    [KeyAsName]
    public CredentialProof.GoshujinClass Credentials { get; private set; } = new();

    private ConcurrentDictionary<string, SignaturePublicKey> aliasToPublicKey = new();
    private ConcurrentDictionary<SignaturePublicKey, string> publicKeyToAlias = new();

    public bool TryGetAlias(SignaturePublicKey publicKey, [MaybeNullWhen(false)] out string alias)
        => this.publicKeyToAlias.TryGetValue(publicKey, out alias);

    public bool TryGetPublicKey(string alias, out SignaturePublicKey publicKey)
        => this.aliasToPublicKey.TryGetValue(alias, out publicKey);
}
