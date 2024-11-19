// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("set")]
public class CustomSubcommandSet : ISimpleCommandAsync<CustomSubcommandSetOptions>
{
    public CustomSubcommandSet(ILogger<CustomSubcommandSet> logger, VaultControl vaultControl)
    {
        this.logger = logger;
        this.vaultControl = vaultControl;
    }

    public async Task RunAsync(CustomSubcommandSetOptions option, string[] args)
    {
        var name = CustomizedCommand.GetName(option.Name);
        if (!this.vaultControl.Exists(name))
        {
            this.logger.TryGet()?.Log(Hashed.Custom.NotFound, option.Name);
            return;
        }

        var custom = new CustomizedCommand(option.Command, args);
        this.vaultControl.SerializeAndAdd(name, custom);
        this.logger.TryGet()?.Log(Hashed.Custom.Set, option.Name);
    }

    private ILogger<CustomSubcommandSet> logger;
    private VaultControl vaultControl;
}

public record CustomSubcommandSetOptions
{
    [SimpleOption("Name", Description = "Customized command name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("Command", Description = "Command", Required = true)]
    public string Command { get; init; } = string.Empty;
}
