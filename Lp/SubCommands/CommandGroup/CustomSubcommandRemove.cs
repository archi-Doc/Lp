// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("rm")]
    public class CustomSubcommandRemove : ISimpleCommandAsync<CustomSubcommandNameOptions>
    {
        public CustomSubcommandRemove(ILogger<CustomSubcommandRemove> logger, VaultControl vaultControl)
        {
            this.vaultControl = vaultControl;
            this.logger = logger;
        }

        public async Task RunAsync(CustomSubcommandNameOptions option, string[] args)
        {
            var name = CustomizedCommand.GetName(option.Name);

            if (this.vaultControl.Root.Remove(name))
            {
                this.logger.TryGet()?.Log(Hashed.Custom.Removed, option.Name);
            }
            else
            {
                this.logger.TryGet(LogLevel.Warning)?.Log(Hashed.Custom.NotFound, option.Name);
            }
        }

        private VaultControl vaultControl;
        private ILogger<CustomSubcommandRemove> logger;
    }

    public record CustomSubcommandNameOptions
    {
        [SimpleOption("Name", Description = "Customized command name", Required = true)]
        public string Name { get; init; } = string.Empty;
    }
}
