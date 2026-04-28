// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("new-command-group")]
    public class NewCommandGroup : ISimpleCommand<Options>
    {
        public NewCommandGroup(ILogger<Options> logger, VaultControl vaultControl)
        {
            this.logger = logger;
            this.vaultControl = vaultControl;
        }

        public async Task Execute(Options options, string[] args, CancellationToken cancellationToken)
        {
            var name = GetName(options.Name);
            if (this.vaultControl.Root.Contains(name))
            {
                this.logger.GetWriter()?.Write(Hashed.CommandGroup.AlreadyExists, options.Name);
                return;
            }

            var commands = SplitLines(options.Command);
            if (this.vaultControl.Root.TryAdd(name, commands, out _))
            {
                this.logger.GetWriter()?.Write(Hashed.CommandGroup.Created, options.Name);
                ShowCommands(commands, this.logger);
            }
            else
            {
                this.logger.GetWriter()?.Write(Hashed.CommandGroup.AlreadyExists, options.Name);
            }
        }

        private readonly ILogger logger;
        private readonly VaultControl vaultControl;
    }
}
