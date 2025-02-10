// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("change-command-group")]
    public class ChangeCommandGroup : ISimpleCommandAsync<NewOptions>
    {
        public ChangeCommandGroup(ILogger<ChangeCommandGroup> logger, VaultControl vaultControl)
        {
            this.logger = logger;
            this.vaultControl = vaultControl;
        }

        public async Task RunAsync(NewOptions options, string[] args)
        {
            var name = GetName(options.Name);
            if (!this.vaultControl.Root.Contains(name))
            {
                this.logger.TryGet()?.Log(Hashed.Custom.NotFound, options.Name);
                return;
            }

            var commands = SimpleParserHelper.SeparateArguments(options.Command);
            this.vaultControl.Root.Add(name, commands);
            this.logger.TryGet()?.Log(Hashed.Custom.Set, options.Name);
            ShowCommands(commands, this.logger);
        }

        private readonly ILogger logger;
        private readonly VaultControl vaultControl;
    }
}
