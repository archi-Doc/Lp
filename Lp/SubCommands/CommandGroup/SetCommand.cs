// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("set-command-group")]
    public class SetCommand : ISimpleCommandAsync<NewOptions>
    {
        public SetCommand(ILogger<SetCommand> logger, VaultControl vaultControl)
        {
            this.logger = logger;
            this.vaultControl = vaultControl;
        }

        public async Task RunAsync(NewOptions option, string[] args)
        {
            var name = GetName(option.Name);
            if (!this.vaultControl.Root.Contains(name))
            {
                this.logger.TryGet()?.Log(Hashed.Custom.NotFound, option.Name);
                return;
            }

            var commands = SimpleParserHelper.SeparateArguments(option.Command);
            this.vaultControl.Root.TryAdd(name, commands, out _);
            this.logger.TryGet()?.Log(Hashed.Custom.Set, option.Name);
            ShowCommands(commands, this.logger);
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
