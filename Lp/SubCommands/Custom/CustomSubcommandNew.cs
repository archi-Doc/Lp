// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("new")]
public class CustomSubcommandNew : ISimpleCommandAsync<CustomSubcommandNewOptions>
{
    public CustomSubcommandNew(ILogger<CustomSubcommandNew> logger, VaultControl vaultControl)
    {
        this.logger = logger;
        this.vaultControl = vaultControl;
    }

    public async Task RunAsync(CustomSubcommandNewOptions option, string[] args)
    {
        var name = CustomizedCommand.GetName(option.Name);
        if (this.vaultControl.Exists(name))
        {
            this.logger.TryGet()?.Log(Hashed.Custom.AlreadyExists, option.Name);
            return;
        }

        var custom = new CustomizedCommand(option.Command, args);
        if (this.vaultControl.SerializeAndTryAdd(name, custom))
        {
            this.logger.TryGet()?.Log(Hashed.Custom.Created, option.Name);
            this.logger.TryGet()?.Log(custom.Command);
        }
        else
        {
            this.logger.TryGet()?.Log(Hashed.Custom.AlreadyExists, option.Name);
        }
    }

    private readonly ILogger logger;
    private readonly VaultControl vaultControl;
}

public record CustomSubcommandNewOptions
{
    [SimpleOption("Name", Description = "Customized command name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("Command", Description = "Command")]
    public string Command { get; init; } = string.Empty;

    /*[SimpleOption("array", Description = "Command array")]
    public string[]? CommandArray { get; init; }*/
}
