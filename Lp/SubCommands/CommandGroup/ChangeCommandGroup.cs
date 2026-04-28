// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("change-command-group")]
    public class ChangeCommandGroup : ISimpleCommand<Options>
    {
        public ChangeCommandGroup(ILogger<ChangeCommandGroup> logger, VaultControl vaultControl)
        {
            this.logger = logger;
            this.vaultControl = vaultControl;
        }

        public async Task Execute(Options options, string[] args, CancellationToken cancellationToken)
        {
            var name = GetName(options.Name);
            /*if (!this.vaultControl.Root.Contains(name))
            {
                this.logger.GetWriter()?.Write(Hashed.Custom.NotFound, options.Name);
                return;
            }*/

            var commands = BaseHelper.SplitLines(options.Command);
            this.vaultControl.Root.Add(name, commands);
            this.logger.GetWriter()?.Write(Hashed.CommandGroup.Set, options.Name);
            ShowCommands(commands, this.logger);
        }

        private readonly ILogger logger;
        private readonly VaultControl vaultControl;
    }
}
