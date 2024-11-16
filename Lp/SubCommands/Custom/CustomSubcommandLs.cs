// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("ls")]
public class CustomSubcommandLs : ISimpleCommandAsync
{
    public CustomSubcommandLs(VaultControl vault, IUserInterfaceService userInterfaceService)
    {
        this.vault = vault;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.vault.GetNames(CustomizedCommand.Prefix).Select(x => x.Substring(CustomizedCommand.Prefix.Length)).ToArray();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private VaultControl vault;
    private IUserInterfaceService userInterfaceService;
}
