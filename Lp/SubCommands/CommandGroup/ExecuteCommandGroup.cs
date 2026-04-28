// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("execute-command-group")]
    public class ExecuteCommandGroup : ISimpleCommand<ExecuteOptions>
    {
        public ExecuteCommandGroup(ILogger<ExecuteCommandGroup> logger, IUserInterfaceService userInterfaceService, LpUnit lpUnit, VaultControl vaultControl)
        {
            this.userInterfaceService = userInterfaceService;
            this.lpUnit = lpUnit;
            this.vaultControl = vaultControl;
            this.logger = logger;
        }

        public async Task Execute(ExecuteOptions option, string[] args, CancellationToken cancellationToken)
        {
            var name = GetName(option.Name);
            if (!this.vaultControl.Root.TryGet<string[]>(name, out var commands, out _))
            {
                this.logger.GetWriter(LogLevel.Warning)?.Write(Hashed.CommandGroup.NotFound, option.Name);
                return;
            }

            if (commands is not null)
            {
                foreach (var x in commands)
                {
                    if (!string.IsNullOrEmpty(x))
                    {
                        this.userInterfaceService.WriteLine(x);
                    }
                }

                foreach (var x in commands)
                {
                    if (!string.IsNullOrEmpty(x))
                    {
                        this.userInterfaceService.EnqueueLine(x);
                    }
                }

                // this.userInterfaceService.WriteLine($">> {x}");
                // this.lpUnit.Subcommand(x);
            }
        }

        private readonly IUserInterfaceService userInterfaceService;
        private readonly LpUnit lpUnit;
        private readonly VaultControl vaultControl;
        private readonly ILogger logger;
    }
}
