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

    #region FieldAndProperty

    [KeyAsName]
    public CredentialProof.GoshujinClass Credentials { get; private set; } = new();

    private ConcurrentDictionary<string, SignaturePublicKey2> aliasToPublicKey = new();
    private ConcurrentDictionary<SignaturePublicKey2, string> publicKeyToAlias = new();

    #endregion

    public void UpdateAlias()
    {
        using (this.Credentials.LockObject.EnterScope())
        {
        }

        this.aliasToPublicKey.TryAdd(LpConstants.LpAlias, LpConstants.LpPublicKey);
    }

    public bool TryGetAlias(SignaturePublicKey2 publicKey, [MaybeNullWhen(false)] out string alias)
        => this.publicKeyToAlias.TryGetValue(publicKey, out alias);

    public bool TryGetPublicKey(string alias, out SignaturePublicKey2 publicKey)
        => this.aliasToPublicKey.TryGetValue(alias, out publicKey);
}
