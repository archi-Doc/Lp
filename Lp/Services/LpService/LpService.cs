// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc;
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
        this.conversionOptions = Alias.Instance;
    }

    public async Task<Authority?> ParseSeedKeyAndCredit(ILogger? logger, string source)
    {//
        var memory = source.AsMemory();
        if (memory.Length > 3 && memory.Span.StartsWith(SeedKeyHelper.PrivateKeyBracket))
        {// SeedKey@Identifier/Mergers
            if (!SeedKey.TryParse(memory.Span, out var seedKey, out var read, this.conversionOptions))
            {
                goto Failure;
            }

            memory = memory.Slice(read);
            if (!Credit.TryParse(memory.Span, out var credit, out read, this.conversionOptions))
            {
                goto Failure;
            }
        }
        else if (memory.Length > 0 && memory.Span[0] != SeedKeyHelper.PublicKeyOpenBracket)
        {// Authority@Ideitifier/Mergers
            var index = memory.Span.IndexOf(LpConstants.CreditSymbol);
            if (index < 0)
            {
                return default;
            }

            var authorityName = memory.Slice(0, index).ToString();
            var authority = await this.authorityControl.GetAuthority(authorityName).ConfigureAwait(false);
            if (authority is null)
            {
                return default;
            }

            memory = memory.Slice(index + 1);
            if (!Credit.TryParse(memory.Span, out var credit, out var read, this.conversionOptions))
            {
                return default;
            }

            var seedKey = authority.GetSeedKey(credit);
        }

Failure:
        return default;
    }

    public async Task<SeedKey?> LoadSeedKey(ILogger? logger, string code)
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

        // Raw string
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
    private readonly IConversionOptions conversionOptions;
}
