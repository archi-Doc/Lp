// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("ls")]
public class CustomSubcommandLs : ISimpleCommandAsync
{
    public CustomSubcommandLs(VaultControl vaultControl, IUserInterfaceService userInterfaceService)
    {
        this.vaultControl = vaultControl;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.vaultControl.Root.GetNames(CustomizedCommand.Prefix).Select(x => x.Substring(CustomizedCommand.Prefix.Length)).ToArray();
        this.userInterfaceService.WriteLine(string.Join(' ', names));
    }

    private readonly VaultControl vaultControl;
    private readonly IUserInterfaceService userInterfaceService;
}
