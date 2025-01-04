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

    public async Task<SeedKey?> GetSeedKey(ILogger? logger, string code)
    {
        SeedKey? seedKey;

        // Authority
        if (await this.authorityControl.GetAuthority(code).ConfigureAwait(false) is { } auth)
        {// Success
            return auth.GetSeedKey();
        }

        // Vault
        if (this.vaultControl.Root.TryGetObject<SeedKey>(code, out seedKey, out var result))
        {// Success
            return seedKey;
        }

        // PrivateKey
        if (SeedKey.TryParse(code, out seedKey))
        {
            return seedKey;
        }

        logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.NoPrivateKey);
        return default;
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly AuthorityControl authorityControl;
    private readonly VaultControl vaultControl;
}
