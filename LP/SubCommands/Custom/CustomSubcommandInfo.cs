// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("info")]
public class CustomSubcommandInfo : ISimpleCommandAsync<CustomSubcommandNameOptions>
{
    public CustomSubcommandInfo(ILogger<CustomSubcommandInfo> logger, Vault vault)
    {
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

        Console.WriteLine($"Command: {option.Name}");
        if (!string.IsNullOrEmpty(command.Command))
        {
            foreach (var x in CustomizedCommand.FromCommandToArray(command.Command))
            {
                if (!string.IsNullOrEmpty(x))
                {
                    Console.WriteLine(x);
                }
            }
        }
    }

    private Vault vault;
    private ILogger<CustomSubcommandInfo> logger;
}
