// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public class LpService
{
    public LpService(IUserInterfaceService userInterfaceService, AuthorityControl authorityControl, VaultControl vaultControl)
    {
        this.userInterfaceService = userInterfaceService;
        this.authorityControl = authorityControl;
        this.vaultControl = vaultControl;
    }

    public async Task<SeedKey?> GetSignaturePrivateKey(ILogger? logger, string authority, string vault, string privateKeyString)
    {
        SeedKey? seedKey;

        if (!string.IsNullOrEmpty(authority))
        {// Authority
            if (await this.authorityControl.GetAuthority(authority).ConfigureAwait(false) is { } auth)
            {
                return auth.GetSeedKey();
            }
            else
            {
                logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotAvailable, authority);
            }
        }

        if (!string.IsNullOrEmpty(vault))
        {// Vault
            if (this.vaultControl.Root.TryGetObject<SeedKey>(vault, out seedKey, out _))
            {
                return seedKey;
            }
            else
            {
                logger?.TryGet(LogLevel.Error)?.Log(Hashed.Vault.NotAvailable, vault);
            }
        }

        if (!string.IsNullOrEmpty(privateKeyString))
        {// PrivateKey
            if (SeedKey.TryParse(privateKeyString, out seedKey))
            {
                return seedKey;
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
    private readonly AuthorityControl authorityControl;
    private readonly VaultControl vaultControl;
}
