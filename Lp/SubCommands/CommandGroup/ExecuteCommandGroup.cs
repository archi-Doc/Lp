// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("execute-command-group")]
    public class ExecuteCommandGroup : ISimpleCommandAsync<ExecuteOptions>
    {
        public ExecuteCommandGroup(ILogger<ExecuteCommandGroup> logger, IUserInterfaceService userInterfaceService, Control control, VaultControl vaultControl)
        {
            this.userInterfaceService = userInterfaceService;
            this.control = control;
            this.vaultControl = vaultControl;
            this.logger = logger;
        }

        public async Task RunAsync(ExecuteOptions option, string[] args)
        {
            var name = GetName(option.Name);
            if (!this.vaultControl.Root.TryGet<string[]>(name, out var commands, out _))
            {
                this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Custom.NotFound, option.Name);
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
                        this.userInterfaceService.EnqueueInput(x);
                    }
                }

                // this.userInterfaceService.WriteLine($">> {x}");
                // this.control.Subcommand(x);
            }
        }

        private readonly IUserInterfaceService userInterfaceService;
        private readonly Control control;
        private readonly VaultControl vaultControl;
        private readonly ILogger logger;
    }

    public record ExecuteOptions
    {
        [SimpleOption("Name", Description = "Command group name", Required = true)]
        public string Name { get; init; } = string.Empty;
    }
}
