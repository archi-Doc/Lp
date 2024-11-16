// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("run")]
public class CustomSubcommandRun : ISimpleCommandAsync<CustomSubcommandNameOptions>
{
    public CustomSubcommandRun(ILogger<CustomSubcommandRun> logger, IUserInterfaceService userInterfaceService, Control control, VaultControl vault)
    {
        this.userInterfaceService = userInterfaceService;
        this.control = control;
        this.vault = vault;
        this.logger = logger;
    }

    public async Task RunAsync(CustomSubcommandNameOptions option, string[] args)
    {
        var name = CustomizedCommand.GetName(option.Name);

        if (!this.vault.TryGetAndDeserialize<CustomizedCommand>(name, out var command))
        {
            this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Custom.NotFound, option.Name);
            return;
        }

        // this.userInterfaceService.WriteLine($"Command: {option.Name}");

        if (!string.IsNullOrEmpty(command.Command))
        {
            foreach (var x in CustomizedCommand.FromCommandToArray(command.Command))
            {
                if (!string.IsNullOrEmpty(x))
                {
                    this.userInterfaceService.WriteLine($">> {x}");
                    this.control.Subcommand(x);
                }
            }
        }
    }

    private IUserInterfaceService userInterfaceService;
    private Control control;
    private VaultControl vault;
    private ILogger<CustomSubcommandRun> logger;
}
