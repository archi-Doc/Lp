// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using LP.Services;
using static LP.AuthorityInterface;

namespace LP;

public class Authority
{
    public const string VaultPrefix = "Authority\\";

    public Authority(IUserInterfaceService userInterfaceService, Vault vault)
    {
        this.UserInterfaceService = userInterfaceService;
        this.vault = vault;
    }

    public string[] GetNames()
        => this.vault.GetNames(VaultPrefix);

    public AuthorityResult TryGetInterface(string name, out AuthorityInterface authorityInterface)
    {
        lock (this.syncObject)
        {
            if (this.nameToAuthorityInterface.TryGetValue(name, out authorityInterface!))
            {
                return AuthorityResult.Success;
            }

            var vaultName = GetVaultName(name);
            if (!this.vault.TryGet(vaultName, out var decrypted))
            {
                return AuthorityResult.NotFound;
            }

            authorityInterface = new AuthorityInterface(this, name, decrypted);
            this.nameToAuthorityInterface.Add(name, authorityInterface);
            return AuthorityResult.Success;
        }
    }

    public AuthorityResult NewAuthority(string name, string passPhrase, AuthorityInfo info)
    {
        var vaultName = GetVaultName(name);

        lock (this.syncObject)
        {
            if (this.vault.Exists(vaultName))
            {
                return AuthorityResult.AlreadyExists;
            }

            var encrypted = PasswordEncrypt.Encrypt(TinyhandSerializer.Serialize(info), passPhrase);
            if (this.vault.TryAdd(vaultName, encrypted))
            {
                return AuthorityResult.Success;
            }
            else
            {
                return AuthorityResult.AlreadyExists;
            }
        }
    }

    public bool Exists(string name)
        => this.vault.Exists(GetVaultName(name));

    public AuthorityResult RemoveAuthority(string name)
    {
        lock (this.syncObject)
        {
            var authorityRemoved = this.nameToAuthorityInterface.Remove(name);
            var vaultRemoved = this.vault.Remove(GetVaultName(name));

            if (vaultRemoved)
            {
                return AuthorityResult.Success;
            }
            else
            {
                return AuthorityResult.NotFound;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetVaultName(string name) => VaultPrefix + name;

#pragma warning disable SA1401
    internal IUserInterfaceService UserInterfaceService;
#pragma warning restore SA1401
    private Vault vault;
    private object syncObject = new();
    private Dictionary<string, AuthorityInterface> nameToAuthorityInterface = new();
}
