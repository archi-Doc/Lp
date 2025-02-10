// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("show-command-group")]
    public class ShowCommandGroup : ISimpleCommandAsync<ExecuteOptions>
    {
        public ShowCommandGroup(ILogger<ShowCommandGroup> logger, VaultControl vaultControl)
        {
            this.logger = logger;
            this.vaultControl = vaultControl;
        }

        public async Task RunAsync(ExecuteOptions option, string[] args)
        {
            var name = GetName(option.Name);
            if (!this.vaultControl.Root.TryGet<string[]>(name, out var commands, out _))
            {
                this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Custom.NotFound, option.Name);
                return;
            }

            ShowCommands(commands, this.logger);
        }

        private readonly ILogger logger;
        private readonly VaultControl vaultControl;
    }
}
