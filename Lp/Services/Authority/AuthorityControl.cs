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
