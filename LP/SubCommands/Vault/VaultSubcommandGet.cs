// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("get")]
public class VaultSubcommandGet : ISimpleCommandAsync<KeyVaultSubcommandDeleteOptions>
{
    public VaultSubcommandGet(IUserInterfaceService userInterfaceService, Vault vault)
    {
        this.userInterfaceService = userInterfaceService;
        this.vault = vault;
    }

    public async Task RunAsync(KeyVaultSubcommandDeleteOptions options, string[] args)
    {
        if (!this.vault.TryGet(options.Name, out var decrypted))
        {
            this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.NotFound, options.Name));
            return;
        }

        this.userInterfaceService.Write($"{options.Name}: ");
        try
        {
            var encoding = new UTF8Encoding(false, true);
            var st = encoding.GetString(decrypted);
            this.userInterfaceService.WriteLine(st);
        }
        catch
        {
            this.userInterfaceService.WriteLine($"byte[{decrypted.Length}]");
        }
    }

    private Vault vault;
    private IUserInterfaceService userInterfaceService;
}
