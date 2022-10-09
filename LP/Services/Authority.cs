// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace LP;

public class Authority
{
    public const string KeyVaultPrefix = "Authority\\";

    public Authority(Vault vault)
    {
        this.vault = vault;
    }

    public AuthorityKeyResult TryGetKey(string name)
    {
        if (this.nameToAuthorityKey.GetOrAdd(name, x =>
        {

        })) ;
    }

    public AuthorityKeyResult NewKey(string name)
    {

    }

    public AuthorityKeyResult RemoveKey(string name)
    {
        this.nameToAuthorityKey.TryRemove(name, out _);
        this.vault.
    }

    private string 

    private Vault vault;
    private ConcurrentDictionary<string, AuthorityKey> nameToAuthorityKey = new();
}
