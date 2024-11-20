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

    public AuthorityControl2(IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public string[] GetNames()
        => this.vaultControl.Root.GetNames(VaultPrefix).Select(x => x.Substring(VaultPrefix.Length)).ToArray();

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
                authority = Authority2.FromVault(vault);
                if (authority.IsExpired())
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

        authority ??= Authority2.FromVault(vault);
        if (password is not null)
        {
            authority.ResetExpirationMics();
        }

        return authority;
    }

    public AuthorityResult NewAuthority(string name, string password, Authority authority)
    {
        var vaultName = GetVaultName(name);

        using (this.lockObject.EnterScope())
        {
            if (this.vaultControl.Root.Exists(vaultName))
            {
                return AuthorityResult.AlreadyExists;
            }

            PasswordEncryption.Encrypt(TinyhandSerializer.Serialize(authority), password, out var encrypted);//
            if (this.vaultControl.Root.TryAddByteArray(vaultName, encrypted, out _))
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
        => this.vaultControl.Root.Exists(GetVaultName(name));

    public AuthorityResult RemoveAuthority(string name)
    {
        using (this.lockObject.EnterScope())
        {
            var authorityRemoved = this.nameToInterface.Remove(name);
            var vaultRemoved = this.vaultControl.Root.Remove(GetVaultName(name));

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

    #region FieldAndProperty

    private readonly IUserInterfaceService userInterfaceService;
    private readonly VaultControl vaultControl;

    #endregion
}
