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

    public AuthorityControl(IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.UserInterfaceService = userInterfaceService;
        this.vaultControl = vaultControl;
    }

    public string[] GetNames()
        => this.vaultControl.Root.GetNames(VaultPrefix).Select(x => x.Substring(VaultPrefix.Length)).ToArray();

    public async Task<Authority?> GetAuthority(string name)
    {
        AuthorityInterface? authorityInterface;
        using (this.lockObject.EnterScope())
        {
            if (!this.nameToInterface.TryGetValue(name, out authorityInterface))
            {// New interface
                var vaultName = GetVaultName(name);
                if (!this.vaultControl.Root.TryGetByteArray(vaultName, out var decrypted, out _))
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

        using (this.lockObject.EnterScope())
        {
            if (this.vaultControl.Root.Exists(vaultName))
            {
                return AuthorityResult.AlreadyExists;
            }

            PasswordEncryption.Encrypt(TinyhandSerializer.Serialize(authority), passPhrase, out var encrypted);
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
    private readonly VaultControl vaultControl;
    private readonly Lock lockObject = new();
    private readonly Dictionary<string, AuthorityInterface> nameToInterface = new();

    #endregion
}
