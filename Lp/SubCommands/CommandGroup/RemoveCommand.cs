﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("remove-command-group")]
    public class RemoveCommand : ISimpleCommandAsync<ExecuteOptions>
    {
        public RemoveCommand(ILogger<ExecuteOptions> logger, VaultControl vaultControl)
        {
            this.vaultControl = vaultControl;
            this.logger = logger;
        }

        public async Task RunAsync(ExecuteOptions option, string[] args)
        {
            var name = GetName(option.Name);
            if (this.vaultControl.Root.Remove(name))
            {
                this.logger.TryGet()?.Log(Hashed.Custom.Removed, option.Name);
            }
            else
            {
                this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Custom.NotFound, option.Name);
            }
        }

        private readonly ILogger logger;
        private readonly VaultControl vaultControl;
    }
}
