// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Lp.Services;

namespace Lp.T3cs;

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

    public async Task<Authority?> GetAuthority(string name, string? password = null)
    {
        var vaultName = GetVaultName(name);
        Vault? vault;
        Authority? authority = default;
        if (password is not null)
        {// Password is specified.
            if (!this.vaultControl.Root.TryGetVault(vaultName, password, out vault))
            {
                return default;
            }
        }
        else
        {// Not specified.
            if (this.vaultControl.Root.TryGetVault(vaultName, string.Empty, out vault))
            {
                authority = Authority.GetFromVault(vault);
                if (authority is null ||
                    authority.IsExpired())
                {
                    vault = null;
                }
            }

            while (vault is null)
            {
                password = await this.userInterfaceService.RequestPassword(Hashed.Authority.EnterPassword, name).ConfigureAwait(false);
                if (password == null)
                {// Canceled
                    return default;
                }

                this.vaultControl.Root.TryGetVault(vaultName, password, out vault);
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
