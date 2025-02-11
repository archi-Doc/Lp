// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("remove-command-group")]
    public class RemoveCommandGroup : ISimpleCommandAsync<ExecuteOptions>
    {
        public RemoveCommandGroup(ILogger<ExecuteOptions> logger, VaultControl vaultControl)
        {
            this.vaultControl = vaultControl;
            this.logger = logger;
        }

        public async Task RunAsync(ExecuteOptions option, string[] args)
        {
            var name = GetName(option.Name);
            if (this.vaultControl.Root.Remove(name))
            {
                this.logger.TryGet()?.Log(Hashed.CommandGroup.Removed, option.Name);
            }
            else
            {
                this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.CommandGroup.NotFound, option.Name);
            }
        }

        private readonly ILogger logger;
        private readonly VaultControl vaultControl;
    }
}
