// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Netsphere.Crypto;

namespace Lp.Services;

/// <summary>
/// Class used to create/delete authority, and get AuthorityInterface using Vault.
/// </summary>
public class AuthorityControl
{
    public const string VaultPrefix = "Authority\\";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetVaultName(string name) => VaultPrefix + name;

    #region FieldAndProperty

    private readonly IUserInterfaceService userInterfaceService;
    private readonly VaultControl vaultControl;

    #endregion

    public AuthorityControl(IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public async Task<SeedKey?> GetLpSeedKey(ILogger? logger)
    {
        var authority = await this.GetLpAuthority(logger).ConfigureAwait(false);
        return authority?.GetSeedKey();
    }

    public async Task<Authority?> GetLpAuthority(ILogger? logger)
    {
        var name = LpConstants.LpKeyAlias;

        var authority = await this.GetAuthority(name).ConfigureAwait(false);
        if (authority is null)
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, name);
            return default;
        }

        var publicKey = authority.GetSignaturePublicKey();
        if (!publicKey.Equals(LpConstants.LpPublicKey))
        {
            logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.KeyMismatch);
            return default;
        }

        return authority;
    }

    public async Task<SeedKey?> GetSeedKey(string name)
    {
        var authority = await this.GetAuthority(name).ConfigureAwait(false);
        return authority?.GetSeedKey();
    }

    /// <summary>
    /// Retrieves an <see cref="Authority"/> instance by name, optionally using a password.
    /// </summary>
    /// <param name="name">The name of the authority to retrieve.</param>
    /// <param name="password">
    /// If a password is non-null, the Authority is returned only if the password is correct.<br/>
    /// If null is specified for the password, the method will return the Authority directly if it is available.<br/>
    /// If the Authority is encrypted, it will attempt to decrypt it using an empty string as the password.<br/>
    /// If decryption fails, the user will be prompted to enter a password.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains the <see cref="Authority"/> if found and accessible; otherwise, <c>null</c>.
    /// </returns>
    public async Task<Authority?> GetAuthority(string name, string? password = null)
    {
        var vaultName = GetVaultName(name);
        Vault? vault;
        Authority? authority = default;
        if (password is not null)
        {// Password is specified.
            if (!this.vaultControl.Root.TryGetVault(vaultName, password, out vault, out var result))
            {
                return default;
            }
        }
        else
        {// Not specified.
            if (this.vaultControl.Root.TryGetVault(vaultName, null, out vault, out var result))
            {
                authority = Authority.GetFromVault(vault);
                if (authority is null || authority.IsExpired)
                {
                    vault = null;
                }
            }

            while (vault is null)
            {
                if (result != VaultResult.PasswordMismatch && result != VaultResult.PasswordRequired)
                {
                    return default;
                }

                password = await this.userInterfaceService.RequestPassword(Hashed.Authority.EnterPassword, name).ConfigureAwait(false);
                if (password == null)
                {// Canceled
                    return default;
                }

                this.vaultControl.Root.TryGetVault(vaultName, password, out vault, out result);
            }
        }

        authority ??= Authority.GetFromVault(vault);
        if (password is not null)
        {
            authority?.ResetExpirationMics();
        }

        return authority;
    }

    public string[] GetNames()
        => this.vaultControl.Root.GetNames(VaultPrefix).Select(x => x.Substring(VaultPrefix.Length)).ToArray();

    public bool Exists(string name)
        => this.vaultControl.Root.Contains(GetVaultName(name));

    public bool NewAuthority(string name, string password, Authority authority)
    {
        var vaultName = GetVaultName(name);
        if (!this.vaultControl.Root.TryAddVault(vaultName, out var vault, out _))
        {
            return false;
        }

        authority.Vault = vault;
        vault.SetPassword(password);
        vault.AddObject(Authority.Name, authority);
        return true;
    }

    public bool RemoveAuthority(string name)
    {
        var vaultName = GetVaultName(name);
        if (this.vaultControl.Root.Remove(vaultName))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
