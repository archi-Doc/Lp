// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Class used to create/delete authority, and get AuthorityInterface using Vault.
/// </summary>
public class AuthorityVault
{
    public const string VaultPrefix = "Authority\\";

    public AuthorityVault(IUserInterfaceService userInterfaceService, Vault vault)
    {
        this.UserInterfaceService = userInterfaceService;
        this.vault = vault;
    }

    public string[] GetNames()
        => this.vault.GetNames(VaultPrefix).Select(x => x.Substring(VaultPrefix.Length)).ToArray();

    public async Task<Authority?> GetAuthority(string name)
    {
        AuthorityInterface? authorityInterface;
        lock (this.syncObject)
        {
            if (!this.nameToInterface.TryGetValue(name, out authorityInterface))
            {// New interface
                var vaultName = GetVaultName(name);
                if (!this.vault.TryGet(vaultName, out var decrypted))
                {// Not found
                    return null;
                }

                authorityInterface = new AuthorityInterface(this, name, decrypted);
                this.nameToInterface.Add(name, authorityInterface);
            }
        }

        return await authorityInterface.Prepare().ConfigureAwait(false);
    }

    public AuthorityResult NewAuthority(string name, string passPhrase, Authority authority)
    {
        var vaultName = GetVaultName(name);

        lock (this.syncObject)
        {
            if (this.vault.Exists(vaultName))
            {
                return AuthorityResult.AlreadyExists;
            }

            var encrypted = PasswordEncryption.Encrypt(TinyhandSerializer.Serialize(authority), passPhrase);
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
            var authorityRemoved = this.nameToInterface.Remove(name);
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

    public async Task<Authority?> GetLpAuthority(ILogger? logger)
    {
        var authority = await this.GetAuthority(LpConstants.LpAlias).ConfigureAwait(false);
        if (authority == null ||
            !authority.PublicKey.Equals(LpConstants.LpPublicKey))
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, LpConstants.LpAlias);
            return default;
        }

        return authority;
    }

    #region FieldAndProperty

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetVaultName(string name) => VaultPrefix + name;

#pragma warning disable SA1401
    internal IUserInterfaceService UserInterfaceService;
#pragma warning restore SA1401
    private Vault vault;
    private object syncObject = new();
    private Dictionary<string, AuthorityInterface> nameToInterface = new();

    #endregion
}
