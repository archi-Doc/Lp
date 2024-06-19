// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("set")]
public class CustomSubcommandSet : ISimpleCommandAsync<CustomSubcommandSetOptions>
{
    public CustomSubcommandSet(ILogger<CustomSubcommandSet> logger, Vault vault)
    {
        this.logger = logger;
        this.vault = vault;
    }

    public async Task RunAsync(CustomSubcommandSetOptions option, string[] args)
    {
        var name = CustomizedCommand.GetName(option.Name);
        if (!this.vault.Exists(name))
        {
            this.logger.TryGet()?.Log(Hashed.Custom.NotFound, option.Name);
            return;
        }

        var custom = new CustomizedCommand(option.Command, args);
        this.vault.SerializeAndAdd(name, custom);
        this.logger.TryGet()?.Log(Hashed.Custom.Set, option.Name);
    }

    private ILogger<CustomSubcommandSet> logger;
    private Vault vault;
}

public record CustomSubcommandSetOptions
{
    [SimpleOption("Name", Description = "Customized command name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("Command", Description = "Command", Required = true)]
    public string Command { get; init; } = string.Empty;
}
