// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class Batch
{
    [SimpleCommand("change-batch")]
    public class ChangeBatch : ISimpleCommand<Options>
    {
        private readonly ILogger logger;
        private readonly VaultControl vaultControl;

        public ChangeBatch(ILogger<ChangeBatch> logger, VaultControl vaultControl)
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
    }
}
