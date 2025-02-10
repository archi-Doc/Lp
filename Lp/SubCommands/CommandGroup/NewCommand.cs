// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("new-command-group")]
    public class NewCommand : ISimpleCommandAsync<NewOptions>
    {
        public NewCommand(ILogger<NewOptions> logger, VaultControl vaultControl)
        {
            this.logger = logger;
            this.vaultControl = vaultControl;
        }

        public async Task RunAsync(NewOptions options, string[] args)
        {
            var name = GetName(options.Name);
            if (this.vaultControl.Root.Contains(name))
            {
                this.logger.TryGet()?.Log(Hashed.Custom.AlreadyExists, options.Name);
                return;
            }

            var commands = SimpleParserHelper.SeparateArguments(options.Command);
            if (this.vaultControl.Root.TryAdd(name, commands, out _))
            {
                this.logger.TryGet()?.Log(Hashed.Custom.Created, options.Name);
                ShowCommands(commands, this.logger);
            }
            else
            {
                this.logger.TryGet()?.Log(Hashed.Custom.AlreadyExists, options.Name);
            }
        }

        private readonly ILogger logger;
        private readonly VaultControl vaultControl;
    }

    public record NewOptions
    {
        [SimpleOption("Name", Description = "Command group name", Required = true)]
        public string Name { get; init; } = string.Empty;

        [SimpleOption("Command", Description = "Command group separated by a separator '|'", Required = true)]
        public string Command { get; init; } = string.Empty;
    }
}
