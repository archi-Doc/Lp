// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

public partial class CommandGroup
{
    [SimpleCommand("list-command")]
    public class ListCommand : ISimpleCommand
    {
        private readonly VaultControl vaultControl;
        private readonly IUserInterfaceService userInterfaceService;

        public ListCommand(VaultControl vaultControl, IUserInterfaceService userInterfaceService)
        {
            this.vaultControl = vaultControl;
            this.userInterfaceService = userInterfaceService;
        }

        public async Task Execute(string[] args, CancellationToken cancellationToken)
        {
            var names = this.vaultControl.Root.GetNames(CommandGroup.Prefix).Select(x => x.Substring(CommandGroup.Prefix.Length)).ToArray();
            this.userInterfaceService.WriteLine(string.Join(' ', names));
        }
    }
}
