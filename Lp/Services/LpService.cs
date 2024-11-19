// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public class LpService
{
    public LpService(IUserInterfaceService userInterfaceService, AuthorityControl authorityVault, VaultControl vaultControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.authorityVault = authorityVault;
        this.vaultControl = vaultControl;
    }

    public async Task<SignaturePrivateKey?> GetSignaturePrivateKey(ILogger? logger, string authority, string vault, string privateKeyString)
    {
        SignaturePrivateKey? privateKey;

        if (!string.IsNullOrEmpty(authority))
        {// Authority
            if (await this.authorityVault.GetAuthority(authority).ConfigureAwait(false) is { } auth)
            {
                return auth.UnsafeGetPrivateKey();
            }
            else
            {
                logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotAvailable, vault);
            }
        }

        if (!string.IsNullOrEmpty(vault))
        {// Vault
            if (this.vaultControl.Root.TryGetObject<SignaturePrivateKey>(vault, out privateKey, out _))
            {
                return privateKey;
            }
            else
            {
                logger?.TryGet(LogLevel.Error)?.Log(Hashed.Vault.NotAvailable, vault);
            }
        }

        if (!string.IsNullOrEmpty(privateKeyString))
        {// PrivateKey
            if (SignaturePrivateKey.TryParse(privateKeyString, out privateKey))
            {
                return privateKey;
            }
            else
            {
                logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.InvalidPrivateKey);
            }
        }

        logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.NoPrivateKey);
        return default;
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly AuthorityControl authorityVault;
    private readonly VaultControl vaultControl;
}
