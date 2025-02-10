// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("set-command-group")]
    public class SetCommand : ISimpleCommandAsync<CustomSubcommandSetOptions>
    {
        public SetCommand(ILogger<SetCommand> logger, VaultControl vaultControl)
        {
            this.logger = logger;
            this.vaultControl = vaultControl;
        }

        public async Task RunAsync(CustomSubcommandSetOptions option, string[] args)
        {
            var name = CustomizedCommand.GetName(option.Name);
            if (!this.vaultControl.Root.Contains(name))
            {
                this.logger.TryGet()?.Log(Hashed.Custom.NotFound, option.Name);
                return;
            }

            var custom = new CustomizedCommand(option.Command, args);
            this.vaultControl.Root.AddObject(name, custom);
            this.logger.TryGet()?.Log(Hashed.Custom.Set, option.Name);
        }

        private readonly ILogger logger;
        private readonly VaultControl vaultControl;
    }

    public record CustomSubcommandSetOptions
    {
        [SimpleOption("Name", Description = "Command group name", Required = true)]
        public string Name { get; init; } = string.Empty;

        [SimpleOption("Command", Description = "Command", Required = true)]
        public string Command { get; init; } = string.Empty;
    }
}
