// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Lp.Services;

namespace Lp.T3cs;

/// <summary>
/// Class used to create/delete authority, and get AuthorityInterface using Vault.
/// </summary>
public class AuthorityControl2
{
    public const string VaultPrefix = "Authority\\";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetVaultName(string name) => VaultPrefix + name;

    #region FieldAndProperty

    private readonly IUserInterfaceService userInterfaceService;
    private readonly VaultControl vaultControl;

    #endregion

    public AuthorityControl2(IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public async Task<Authority2?> GetAuthority(string name, string? password = null)
    {
        var vaultName = GetVaultName(name);
        Vault? vault;
        Authority2? authority = default;
        if (password is not null)
        {// Password is specified.
            if (!this.vaultControl.Root.TryGetVault(vaultName, password, out vault))
            {
                return default;
            }
        }
        else
        {// Not specified.
            if (this.vaultControl.Root.TryGetVault(vaultName, null, out vault))
            {
                authority = Authority2.GetFromVault(vault);
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

        authority ??= Authority2.GetFromVault(vault);
        if (password is not null)
        {
            authority?.ResetExpirationMics();
        }

        return authority;
    }

    public string[] GetNames()
        => this.vaultControl.Root.GetNames(VaultPrefix).Select(x => x.Substring(VaultPrefix.Length)).ToArray();

    public bool Exists(string name)
        => this.vaultControl.Root.Exists(GetVaultName(name));

    public AuthorityResult NewAuthority(string name, string password, Authority authority)
    {
        var vaultName = GetVaultName(name);
        if (!this.vaultControl.Root.TryAddVault(vaultName, out var vault, out _))
        {
            return AuthorityResult.AlreadyExists;
        }

        vault.SetPassword(password);
        vault.AddObject(Authority2.Name, authority);
        return AuthorityResult.Success;
    }

    public AuthorityResult RemoveAuthority(string name)
    {
        var vaultName = GetVaultName(name);
        if (this.vaultControl.Root.Remove(vaultName))
        {
            return AuthorityResult.Success;
        }
        else
        {
            return AuthorityResult.NotFound;
        }
    }
}
