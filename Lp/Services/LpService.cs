// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public class LpService
{
    public LpService(IUserInterfaceService userInterfaceService, AuthorityControl authorityVault, VaultControl vault)
    {
        this.userInterfaceService = userInterfaceService;
        this.authorityVault = authorityVault;
        this.vault = vault;
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
            if (this.vault.TryGetAndDeserialize<SignaturePrivateKey>(vault, out privateKey))
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
    private readonly VaultControl vault;
}
