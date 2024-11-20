// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("info")]
public class CustomSubcommandInfo : ISimpleCommandAsync<CustomSubcommandNameOptions>
{
    public CustomSubcommandInfo(ILogger<CustomSubcommandInfo> logger, IUserInterfaceService userInterfaceService, VaultControl vaultControl)
    {
        this.vaultControl = vaultControl;
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
    }

    public async Task RunAsync(CustomSubcommandNameOptions option, string[] args)
    {
        var name = CustomizedCommand.GetName(option.Name);

        if (!this.vaultControl.Root.TryGetObject<CustomizedCommand>(name, out var command, out _))
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Custom.NotFound, option.Name);
            return;
        }

        this.userInterfaceService.WriteLine($"Command: {option.Name}");
        if (!string.IsNullOrEmpty(command.Command))
        {
            foreach (var x in CustomizedCommand.FromCommandToArray(command.Command))
            {
                if (!string.IsNullOrEmpty(x))
                {
                    this.userInterfaceService.WriteLine(x);
                }
            }
        }
    }

    private readonly VaultControl vaultControl;
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
}
